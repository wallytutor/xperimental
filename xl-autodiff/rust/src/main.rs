pub mod autodiff;
pub mod data;
pub mod thermo;

use autodiff::{diff, Dual};
use data::{get_calcite, get_co2, get_diaspore, get_h2o, get_lime};
use thermo::{cp, enthalpy, entropy, gibbs, T_REF};

// ------------------------------------------------------------------------------------------------
// Main
// ------------------------------------------------------------------------------------------------

fn main() {
    sample_thermo_evaluation();
    sample_autodiff_evaluation();
    sample_equilibrium_evaluation();
    sample_composition_tabulation();
}

// ... unchanged functions up to sample_equilibrium_evaluation ...
// Wait, I must replace the exact block. Let's do it carefully.

fn sample_thermo_evaluation() {
    println!("=== Thermodynamic Properties ===\n");

    let calcite = get_calcite();
    let lime = get_lime();
    let co2 = get_co2();

    let species = [&calcite, &lime, &co2];
    for (i, s) in species.iter().enumerate() {
        let names = ["Calcite", "Lime", "CO2"];
        println!("--- {} ---", names[i]);
        let coefs: Vec<f64> = s.unpack_coefs();
        println!("Cp ...........: {:.6}", cp(&coefs, 298.15));
        println!(
            "Enthalpy .....: {:.6}",
            enthalpy(T_REF, s.delta_hf, &coefs, 300.0)
        );
        println!("Entropy ......: {:.6}", entropy(T_REF, s.s0, &coefs, 300.0));
        println!(
            "Gibbs ........: {:.6}\n",
            gibbs(T_REF, s.delta_hf, s.s0, &coefs, 300.0)
        );
    }
}

fn sample_autodiff_evaluation() {
    let calcite = get_calcite();
    let g = |t: Dual<f64>| {
        let coefs: Vec<Dual<f64>> = calcite.unpack_coefs();
        gibbs(
            Dual::constant(T_REF),
            Dual::constant(calcite.delta_hf),
            Dual::constant(calcite.s0),
            &coefs,
            t,
        )
    };

    let dg = diff(g, 300.0);
    println!("\nAutodiff Verification (Calcite):");
    println!("dG/dT = {:.6}", dg);
    println!(
        "-S(T) = {:.6}",
        -entropy(T_REF, calcite.s0, &calcite.unpack_coefs::<f64>(), 300.0)
    );
}

fn find_particular_solution(a: &[Vec<f64>], b: &[f64], n_s: usize, n_e: usize) -> Vec<f64> {
    let mut phi = vec![0.0; n_s];
    let lr = 0.01;
    for _ in 0..20000 {
        let mut grad = vec![0.0; n_s];
        for i in 0..n_e {
            let mut err = -b[i];
            for k in 0..n_s {
                err += a[i][k] * phi[k];
            }
            for k in 0..n_s {
                grad[k] += err * a[i][k];
            }
        }
        for k in 0..n_s {
            phi[k] -= lr * grad[k];
        }
    }
    phi
}

fn find_nullspace(a: &[Vec<f64>], n_s: usize, n_e: usize) -> Vec<f64> {
    let mut v = vec![1.0; n_s];
    let lr = 0.01;
    for _ in 0..20000 {
        let mut grad = vec![0.0; n_s];
        for i in 0..n_e {
            let mut err = 0.0;
            for k in 0..n_s {
                err += a[i][k] * v[k];
            }
            for k in 0..n_s {
                grad[k] += err * a[i][k];
            }
        }
        for k in 0..n_s {
            v[k] -= lr * grad[k];
        }

        let mut norm = 0.0;
        for k in 0..n_s {
            norm += v[k] * v[k];
        }
        norm = norm.sqrt();
        if norm > 1e-12 {
            for k in 0..n_s {
                v[k] /= norm;
            }
        }
    }
    v
}

pub fn evaluate_local_equilibrium(
    species: &[&thermo::Substance],
    elements: &[&str],
    b: &[f64],
    t: f64,
    p: f64,
) -> Vec<f64> {
    let n_s = species.len();
    let n_e = elements.len();
    let r = 8.314_f64;

    // Compute g_k
    let mut g_k = vec![0.0; n_s];
    for i in 0..n_s {
        let s = species[i];
        let mut g = gibbs(T_REF, s.delta_hf, s.s0, &s.unpack_coefs::<f64>(), t);
        if s.molar_volume > 20.0 {
            // Gas heuristic
            g += r * t * p.ln();
        }
        g_k[i] = g;
    }

    // Build stoichiometry matrix A
    let mut a = vec![vec![0.0; n_s]; n_e];
    for i in 0..n_e {
        for j in 0..n_s {
            a[i][j] = species[j].elements.get(elements[i]).copied().unwrap_or(0.0);
        }
    }

    let phi_p = find_particular_solution(&a, b, n_s, n_e);
    let v = find_nullspace(&a, n_s, n_e);

    // Check if v is actually in the nullspace
    let mut v_err = 0.0;
    for i in 0..n_e {
        let mut err = 0.0;
        for k in 0..n_s {
            err += a[i][k] * v[k];
        }
        v_err += err * err;
    }

    if v_err > 1e-4 {
        // 0 degrees of freedom, just return the particular solution
        return phi_p.into_iter().map(|x| x.max(0.0)).collect();
    }

    // 1 degree of freedom (Linear Programming line bounds)
    let mut alpha_min = -1e6;
    let mut alpha_max = 1e6;

    for k in 0..n_s {
        if v[k] > 1e-6 {
            let limit = -phi_p[k] / v[k];
            if limit > alpha_min {
                alpha_min = limit;
            }
        } else if v[k] < -1e-6 {
            let limit = -phi_p[k] / v[k];
            if limit < alpha_max {
                alpha_max = limit;
            }
        }
    }

    if alpha_min > alpha_max {
        return phi_p.into_iter().map(|x| x.max(0.0)).collect();
    }

    let mut phi_min = vec![0.0; n_s];
    let mut phi_max = vec![0.0; n_s];
    let mut g_min = 0.0;
    let mut g_max = 0.0;

    for k in 0..n_s {
        phi_min[k] = (phi_p[k] + alpha_min * v[k]).max(0.0);
        phi_max[k] = (phi_p[k] + alpha_max * v[k]).max(0.0);
        g_min += phi_min[k] * g_k[k];
        g_max += phi_max[k] * g_k[k];
    }

    if g_min < g_max {
        phi_min
    } else {
        phi_max
    }
}

fn sample_equilibrium_evaluation() {
    let calcite = get_calcite();
    let lime = get_lime();
    let co2 = get_co2();
    let diaspore = get_diaspore();
    let h2o = get_h2o();

    let species = [&calcite, &lime, &co2, &diaspore, &h2o];
    let names = ["CaCO3(s)", "CaO(s)", "CO2(g)", "Diaspore(s)", "H2O(g)"];
    let elements = ["Ca", "C", "O", "Al", "H"];

    let t = 1173.15_f64; // K
    let p_user = 1.0_f64; // bar

    // Mixture representing 1 mole of atoms of CaCO3 (5 atoms) + 1 mole of atoms of Diaspore (4 atoms). Total = 9 atoms.
    let b = [1.0 / 9.0, 1.0 / 9.0, 5.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0];

    println!("\n=== Generic CALPHAD Local Equilibrium ===");
    println!("T = {} K, P = {} bar", t, p_user);
    println!("System elements: {:?}", elements);
    println!("System composition: {:?}", b);

    let equilibrium_phi = evaluate_local_equilibrium(&species, &elements, &b, t, p_user);

    println!("\nEquilibrium amounts:");
    for i in 0..5 {
        println!("  {:<12}: {:.6} mol", names[i], equilibrium_phi[i]);
    }
}

fn sample_composition_tabulation() {
    let calcite = get_calcite();
    let lime = get_lime();
    let co2 = get_co2();
    let diaspore = get_diaspore();
    let h2o = get_h2o();

    let species = [&calcite, &lime, &co2, &diaspore, &h2o];
    let elements = ["Ca", "C", "O", "Al", "H"];

    // Mix corresponding to 1 part CaCO3, 1 part Diaspore
    let b = [1.0 / 9.0, 1.0 / 9.0, 5.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0];

    println!("\n=== Composition Tabulation (300 K - 1200 K) ===");
    println!(
        "{:<10} | {:<12} | {:<12} | {:<12} | {:<14} | {:<12}",
        "T (K)", "CaCO3 (mol)", "CaO (mol)", "CO2 (mol)", "Diaspore (mol)", "H2O (mol)"
    );
    println!(
        "{:-<10}-+-{:-<12}-+-{:-<12}-+-{:-<12}-+-{:-<14}-+-{:-<12}-",
        "", "", "", "", "", ""
    );

    let mut t = 300.0;
    while t <= 1200.0 {
        let phi = evaluate_local_equilibrium(&species, &elements, &b, t, 1.0);
        println!(
            "{:<10.2} | {:<12.6} | {:<12.6} | {:<12.6} | {:<14.6} | {:<12.6}",
            t, phi[0], phi[1], phi[2], phi[3], phi[4]
        );
        t += 100.0;
    }
    println!();
}
