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

    let species = [&calcite, &lime, &co2];
    let names = ["CaCO3(s)", "CaO(s)", "CO2(g)"];

    let t = 573.15_f64; // K (approx 900 C)
    let p_user = 1.0_f64; // bar (user provided pressure)
    let r = 8.314_f64; // J/(mol*K)

    // User provided system composition (N = 1 mole of elements)
    // Let's say we have a mixture that corresponds to 1 mole of CaCO3 initially,
    // which has 1 Ca, 1 C, 3 O. To make N=1 mole of atoms:
    // b_Ca = 0.2, b_C = 0.2, b_O = 0.6
    let b_ca = 0.2_f64;
    let b_c = 0.2_f64;
    let b_o = 0.6_f64;
    let n_total_atoms = 1.0_f64;

    println!("\n=== CALPHAD Equilibrium Evaluation ===");
    println!("T = {} K, P = {} bar", t, p_user);
    println!("System composition (mole fractions of elements):");
    println!("  x_Ca = {}, x_C = {}, x_O = {}", b_ca, b_c, b_o);

    // Compute molar Gibbs energies of the phases at T
    // g_k = G_k^0 + RT ln(P) for gases, g_k = G_k^0 for solids
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

    // For CO2, add the pressure term
    let g_co2_p = g_co2_0 + r * t * p_user.ln();

    let g_phases = [g_calcite_0, g_lime_0, g_co2_p];

    // In CALPHAD, we evaluate the Gibbs energy of the system as G = \sum phi_k g_k
    // subject to mass conservation: \sum phi_k a_{jk} = b_j * N_total_atoms
    // For 3 phases and rank 2 composition matrix, the solution lies on a 1D line:
    // phi_calcite = x
    // phi_lime = b_ca * n_total_atoms - x
    // phi_co2 = b_c * n_total_atoms - x

    // We evaluate the valid vertices (where phi_k >= 0)
    let max_x = (b_ca * n_total_atoms).min(b_c * n_total_atoms);

    // Vertex 1: x = 0 (Complete decomposition)
    let phi_v1 = [0.0, b_ca * n_total_atoms, b_c * n_total_atoms];
    let g_v1 = phi_v1[0] * g_phases[0] + phi_v1[1] * g_phases[1] + phi_v1[2] * g_phases[2];

    // Vertex 2: x = max_x (Complete formation of CaCO3)
    let phi_v2 = [
        max_x,
        b_ca * n_total_atoms - max_x,
        b_c * n_total_atoms - max_x,
    ];
    let g_v2 = phi_v2[0] * g_phases[0] + phi_v2[1] * g_phases[1] + phi_v2[2] * g_phases[2];

    println!("\nEvaluating Gibbs Energy of system for valid assemblages (G = \\sum phi_k g_k):");
    println!(
        "  Assemblage 1 (Decomposed): phi = {:?}, G_sys = {:.2} J",
        phi_v1, g_v1
    );
    println!(
        "  Assemblage 2 (Associated): phi = {:?}, G_sys = {:.2} J",
        phi_v2, g_v2
    );

    let equilibrium_phi = if g_v1 < g_v2 { phi_v1 } else { phi_v2 };

    println!("\nEquilibrium amounts (Moles of phases):");
    for i in 0..3 {
        println!("  {:<10}: {:.6} mol", names[i], equilibrium_phi[i]);
    }

    // Check mass conservation
    let mut check_ca = 0.0;
    let mut check_c = 0.0;
    let mut check_o = 0.0;
    for i in 0..3 {
        check_ca += equilibrium_phi[i] * species[i].elements.get("Ca").copied().unwrap_or(0.0);
        check_c += equilibrium_phi[i] * species[i].elements.get("C").copied().unwrap_or(0.0);
        check_o += equilibrium_phi[i] * species[i].elements.get("O").copied().unwrap_or(0.0);
    }

    println!("\nMass conservation check:");
    println!("  Ca: {:.6} == {:.6}", check_ca, b_ca * n_total_atoms);
    println!("  C : {:.6} == {:.6}", check_c, b_c * n_total_atoms);
    println!("  O : {:.6} == {:.6}", check_o, b_o * n_total_atoms);
}
