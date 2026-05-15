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
