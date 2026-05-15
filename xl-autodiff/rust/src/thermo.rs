use crate::autodiff::Numeric;
use std::collections::HashMap;

pub const T_REF: f64 = 298.15;

pub fn cp<T: Numeric>(a: &[T], t: T) -> T {
    a[0] + a[1] * t + a[2] / (t * t)
}

pub fn enthalpy_def<T: Numeric>(a: &[T], t: T) -> T {
    let two = T::from_f64(2.0);
    a[0] * t + (a[1] / two) * t * t - a[2] / t
}

pub fn entropy_def<T: Numeric>(a: &[T], t: T) -> T {
    let two = T::from_f64(2.0);
    a[0] * t.ln() + a[1] * t - (a[2] / two) / (t * t)
}

pub fn enthalpy<T: Numeric>(t_ref: T, delta_hf: T, coefs: &[T], t: T) -> T {
    delta_hf + enthalpy_def(coefs, t) - enthalpy_def(coefs, t_ref)
}

pub fn entropy<T: Numeric>(t_ref: T, s0: T, coefs: &[T], t: T) -> T {
    s0 + entropy_def(coefs, t) - entropy_def(coefs, t_ref)
}

pub fn gibbs<T: Numeric>(t_ref: T, delta_hf: T, s0: T, coefs: &[T], t: T) -> T {
    enthalpy(t_ref, delta_hf, coefs, t) - t * entropy(t_ref, s0, coefs, t)
}

// ------------------------------------------------------------------------------------------------
// Specialized data format
// ------------------------------------------------------------------------------------------------

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum AggregationType {
    Solid,
    Liquid,
    Gas,
}

#[derive(Debug, Clone)]
pub struct Substance {
    pub molar_mass: f64,
    pub molar_volume: f64,
    pub delta_gf: f64,
    pub delta_hf: f64,
    pub s0: f64,
    pub raw_coefs: Vec<f64>,
    pub elements: HashMap<String, f64>,
    pub reference: String,
    pub aggregation_type: AggregationType,
}

impl Substance {
    pub fn unpack_coefs<T: Numeric>(&self) -> Vec<T> {
        self.raw_coefs.iter().map(|&c| T::from_f64(c)).collect()
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::autodiff::{diff, Dual};
    use crate::data::get_calcite;

    #[test]
    fn test_thermo_derivatives() {
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

        let expected = -entropy(T_REF, calcite.s0, &calcite.unpack_coefs::<f64>(), 300.0);
        let actual = diff(g, 300.0);
        assert!(
            (expected - actual).abs() < 1e-9,
            "expected={}, actual={}",
            expected,
            actual
        );
    }
}
