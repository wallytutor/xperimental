use crate::autodiff::Numeric;
use std::collections::HashMap;

pub const T_REF: f64 = 298.15;

pub const R_GAS: f64 = 8.31446261815324;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum AggregationType {
    Solid,
    Liquid,
    Gas,
}

#[derive(Debug, Clone)]
pub enum Parameterization {
    MaierKelley {
        a: f64,
        b: f64,
        c: f64,
    },
    NASA7 {
        a1: f64,
        a2: f64,
        a3: f64,
        a4: f64,
        a5: f64,
        a6: f64,
        a7: f64,
    },
    NASA9 {
        a1: f64,
        a2: f64,
        a3: f64,
        a4: f64,
        a5: f64,
        a6: f64,
        a7: f64,
        a8: f64,
        a9: f64,
    },
    Shomate {
        a: f64,
        b: f64,
        c: f64,
        d: f64,
        e: f64,
        f: f64,
        g: f64,
        h: f64,
    },
    GibbsPolynomial {
        a: f64,
        b: f64,
        c: f64,
        d: f64,
        e: f64,
        f: f64,
        g: f64,
    },
}

#[derive(Debug, Clone)]
pub struct TemperatureRange {
    pub t_min: f64,
    pub t_max: f64,
    pub model: Parameterization,
}

#[derive(Debug, Clone)]
pub struct Substance {
    pub molar_mass: f64,
    pub molar_volume: f64,
    pub delta_gf: f64,
    pub delta_hf: f64,
    pub s0: f64,
    pub ranges: Vec<TemperatureRange>,
    pub elements: HashMap<String, f64>,
    pub reference: String,
    pub aggregation_type: AggregationType,
}

fn cp_maierkelley<T: Numeric>(a: T, b: T, c: T, t: T) -> T {
    a + b * t + c / (t * t)
}

fn cp_nasa7<T: Numeric>(a1: T, a2: T, a3: T, a4: T, a5: T, t: T) -> T {
    let poly = a1 + t * (a2 + t * (a3 + t * (a4 + t * a5)));
    T::from_f64(R_GAS) * poly
}

fn cp_shomate<T: Numeric>(a: T, b: T, c: T, d: T, e: T, t: T) -> T {
    let tt = t / T::from_f64(1000.0);
    let poly = a + tt * (b + tt * (c + tt * d));
    poly + e / (tt * tt)
}

fn enthalpy_maierkelley<T: Numeric>(a: T, b: T, c: T, t: T, t_ref: T, h_ref: T) -> T {
    let half = T::from_f64(0.5);
    let delta_h = a * (t - t_ref)
        + half * b * (t * t - t_ref * t_ref)
        - c * (T::from_f64(1.0) / t - T::from_f64(1.0) / t_ref);
    h_ref + delta_h
}

fn enthalpy_nasa7<T: Numeric>(a1: T, a2: T, a3: T, a4: T, a5: T, a6: T, t: T) -> T {
    let c2 = T::from_f64(1.0 / 2.0);
    let c3 = T::from_f64(1.0 / 3.0);
    let c4 = T::from_f64(1.0 / 4.0);
    let c5 = T::from_f64(1.0 / 5.0);
    let poly = a6 + t * (a1 + t * (c2 * a2 + t * (c3 * a3 + t * (c4 * a4 + t * (c5 * a5)))));
    T::from_f64(R_GAS) * poly
}

fn enthalpy_shomate<T: Numeric>(a: T, b: T, c: T, d: T, e: T, f: T, t: T) -> T {
    let tt = t / T::from_f64(1000.0);
    let c2 = T::from_f64(1.0 / 2.0);
    let c3 = T::from_f64(1.0 / 3.0);
    let c4 = T::from_f64(1.0 / 4.0);
    let poly = f - e / tt + tt * (a + tt * (c2 * b + tt * (c3 * c + tt * (c4 * d))));
    T::from_f64(1000.0) * poly
}

fn entropy_maierkelley<T: Numeric>(a: T, b: T, c: T, t: T, t_ref: T, s_ref: T) -> T {
    let half = T::from_f64(0.5);
    let delta_s = a * (t / t_ref).ln()
        + b * (t - t_ref)
        - half * c * (T::from_f64(1.0) / (t * t) - T::from_f64(1.0) / (t_ref * t_ref));
    s_ref + delta_s
}

fn entropy_nasa7<T: Numeric>(a1: T, a2: T, a3: T, a4: T, a5: T, a7: T, t: T) -> T {
    let c2 = T::from_f64(1.0 / 2.0);
    let c3 = T::from_f64(1.0 / 3.0);
    let c4 = T::from_f64(1.0 / 4.0);
    let poly = a7 + a1 * t.ln() + t * (a2 + t * (c2 * a3 + t * (c3 * a4 + t * (c4 * a5))));
    T::from_f64(R_GAS) * poly
}

fn entropy_shomate<T: Numeric>(a: T, b: T, c: T, d: T, e: T, g: T, t: T) -> T {
    let tt = t / T::from_f64(1000.0);
    let c2 = T::from_f64(1.0 / 2.0);
    let c3 = T::from_f64(1.0 / 3.0);
    let poly =
        g + a * tt.ln() - e / (T::from_f64(2.0) * tt * tt) + tt * (b + tt * (c2 * c + tt * (c3 * d)));
    poly
}

impl Substance {
    pub fn get_range(&self, t: f64) -> &TemperatureRange {
        for r in &self.ranges {
            if t >= r.t_min && t <= r.t_max {
                return r;
            }
        }
        if t < self.ranges[0].t_min {
            return &self.ranges[0];
        }
        self.ranges.last().unwrap()
    }

    pub fn cp<T: Numeric>(&self, t: T) -> T {
        let t_val = t.to_f64();
        let range = self.get_range(t_val);
        match range.model {
            Parameterization::MaierKelley { a, b, c } => {
                cp_maierkelley(T::from_f64(a), T::from_f64(b), T::from_f64(c), t)
            }
            Parameterization::NASA7 {
                a1, a2, a3, a4, a5, ..
            } => cp_nasa7(
                T::from_f64(a1),
                T::from_f64(a2),
                T::from_f64(a3),
                T::from_f64(a4),
                T::from_f64(a5),
                t,
            ),
            Parameterization::Shomate { a, b, c, d, e, .. } => cp_shomate(
                T::from_f64(a),
                T::from_f64(b),
                T::from_f64(c),
                T::from_f64(d),
                T::from_f64(e),
                t,
            ),
            _ => unimplemented!(),
        }
    }

    pub fn enthalpy<T: Numeric>(&self, t: T) -> T {
        let t_val = t.to_f64();
        let range = self.get_range(t_val);
        match range.model {
            Parameterization::MaierKelley { a, b, c } => enthalpy_maierkelley(
                T::from_f64(a),
                T::from_f64(b),
                T::from_f64(c),
                t,
                T::from_f64(T_REF),
                T::from_f64(self.delta_hf),
            ),
            Parameterization::NASA7 {
                a1,
                a2,
                a3,
                a4,
                a5,
                a6,
                ..
            } => enthalpy_nasa7(
                T::from_f64(a1),
                T::from_f64(a2),
                T::from_f64(a3),
                T::from_f64(a4),
                T::from_f64(a5),
                T::from_f64(a6),
                t,
            ),
            Parameterization::Shomate {
                a, b, c, d, e, f, ..
            } => enthalpy_shomate(
                T::from_f64(a),
                T::from_f64(b),
                T::from_f64(c),
                T::from_f64(d),
                T::from_f64(e),
                T::from_f64(f),
                t,
            ),
            _ => unimplemented!(),
        }
    }

    pub fn entropy<T: Numeric>(&self, t: T) -> T {
        let t_val = t.to_f64();
        let range = self.get_range(t_val);

        match range.model {
            Parameterization::MaierKelley { a, b, c } => entropy_maierkelley(
                T::from_f64(a),
                T::from_f64(b),
                T::from_f64(c),
                t,
                T::from_f64(T_REF),
                T::from_f64(self.s0),
            ),
            Parameterization::NASA7 {
                a1,
                a2,
                a3,
                a4,
                a5,
                a7,
                ..
            } => entropy_nasa7(
                T::from_f64(a1),
                T::from_f64(a2),
                T::from_f64(a3),
                T::from_f64(a4),
                T::from_f64(a5),
                T::from_f64(a7),
                t,
            ),
            Parameterization::Shomate {
                a, b, c, d, e, g, ..
            } => entropy_shomate(
                T::from_f64(a),
                T::from_f64(b),
                T::from_f64(c),
                T::from_f64(d),
                T::from_f64(e),
                T::from_f64(g),
                t,
            ),
            _ => unimplemented!(),
        }
    }

    pub fn gibbs<T: Numeric>(&self, t: T) -> T {
        self.enthalpy(t) - t * self.entropy(t)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::autodiff::{Dual, diff};
    use crate::data::{get_calcite, get_h2o};

    #[test]
    fn test_thermo_derivatives() {
        let calcite = get_calcite();
        let g = |t: Dual<f64>| calcite.gibbs(t);

        let expected = -calcite.entropy(300.0);
        let actual = diff(g, 300.0);
        assert!(
            (expected - actual).abs() < 1e-9,
            "expected={}, actual={}",
            expected,
            actual
        );
    }

    #[test]
    fn test_h2o_nasa7() {
        let h2o = get_h2o();
        let val = h2o.cp(300.0);
        assert!(val > 0.0);
    }
}
