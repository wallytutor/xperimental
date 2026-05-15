pub mod autodiff;
pub mod data;
pub mod thermo;

use autodiff::{diff, Dual};
use data::{get_calcite, get_co2, get_lime};
use thermo::{cp, enthalpy, entropy, gibbs, T_REF};

// ------------------------------------------------------------------------------------------------
// Main
// ------------------------------------------------------------------------------------------------

fn main() {
    sample_thermo_evaluation();
    sample_autodiff_evaluation();
    sample_equilibrium_evaluation();
}

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

fn sample_equilibrium_evaluation() {
    let calcite = get_calcite();
    let lime = get_lime();
    let co2 = get_co2();

    // Standard states for the decomposition of Calcite
    // CaCO3(s) <=> CaO(s) + CO2(g)
    let t = 1173.15; // K
    let p_total = 1.0; // bar
    let n_inert = 1.0; // mol (e.g., N2) to allow a partial pressure equilibrium without boundary hits
    let r = 8.314; // J/(mol*K)

    // Compute standard Gibbs free energies at T
    let g_calcite_0 = gibbs(
        T_REF,
        calcite.delta_hf,
        calcite.s0,
        &calcite.unpack_coefs::<f64>(),
        t,
    );
    let g_lime_0 = gibbs(
        T_REF,
        lime.delta_hf,
        lime.s0,
        &lime.unpack_coefs::<f64>(),
        t,
    );
    let g_co2_0 = gibbs(T_REF, co2.delta_hf, co2.s0, &co2.unpack_coefs::<f64>(), t);

    println!("\n=== Equilibrium Evaluation (CaCO3 <=> CaO + CO2) ===");
    println!(
        "T = {} K, P = {} bar, inert gas = {} mol",
        t, p_total, n_inert
    );

    // Objective function: affinity or dG/dx. We want f(x) = 0.
    // f(x) = G0_CaO + G0_CO2(P_CO2) - G0_CaCO3
    // P_CO2 = p_total * x / (x + n_inert)
    // f(x) = (g_lime_0 + g_co2_0 - g_calcite_0) + R*T * ln(x / (x + n_inert))

    // We implement it using Dual to automatically get the exact analytical derivative for Newton-Raphson
    let f = |x: Dual<f64>| -> Dual<f64> {
        let delta_g0 = Dual::constant(g_lime_0 + g_co2_0 - g_calcite_0);
        let rt = Dual::constant(r * t);
        let p = Dual::constant(p_total);
        let inert = Dual::constant(n_inert);

        let p_co2 = p * x / (x + inert);
        delta_g0 + rt * p_co2.ln()
    };

    let mut x = 0.1; // Initial guess for extent of reaction (mol)
    println!("\nNewton-Raphson iterations:");
    for i in 1..=20 {
        let res = f(Dual::variable(x));
        let val = res.value;
        let deriv = res.deriv;

        println!(
            "  Step {}: x = {:.6}, f(x) = {:>10.2}, f'(x) = {:>10.2}",
            i, x, val, deriv
        );

        if val.abs() < 1e-4 {
            println!("  Converged successfully!");
            break;
        }

        let mut step = val / deriv;

        // Prevent huge steps and keep x within valid bounds (0, 1)
        if step > 0.5 {
            step = 0.5;
        }
        if step < -0.5 {
            step = -0.5;
        }

        x = x - step;
        if x <= 1e-6 {
            x = 1e-6;
        }
        if x >= 0.999999 {
            x = 0.999999;
        }
    }

    println!("\nEquilibrium amounts:");
    println!("  CaCO3(s) : {:.6} mol", 1.0 - x);
    println!("  CaO(s)   : {:.6} mol", x);
    println!("  CO2(g)   : {:.6} mol", x);
}
