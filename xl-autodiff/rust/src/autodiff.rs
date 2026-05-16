use std::ops::{Add, Div, Mul, Neg, Sub};

// ------------------------------------------------------------------------------------------------
// AutoDiff - Forward Mode (Robust & Efficient)
// ------------------------------------------------------------------------------------------------

#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Dual<T> {
    pub value: T,
    pub deriv: T,
}

impl<T> Dual<T> {
    pub fn new(value: T, deriv: T) -> Self {
        Self { value, deriv }
    }
}

impl Dual<f64> {
    pub fn constant(value: f64) -> Self {
        Self { value, deriv: 0.0 }
    }

    pub fn variable(value: f64) -> Self {
        Self { value, deriv: 1.0 }
    }

    pub fn pow(self, b: Dual<f64>) -> Self {
        let v = self.value.powf(b.value);
        let d = v * (b.deriv * self.value.ln() + b.value * self.deriv / self.value);
        Self { value: v, deriv: d }
    }

    pub fn powf(self, b: f64) -> Self {
        Self {
            value: self.value.powf(b),
            deriv: b * self.value.powf(b - 1.0) * self.deriv,
        }
    }

    pub fn f_pow(a: f64, b: Self) -> Self {
        let v = a.powf(b.value);
        Self {
            value: v,
            deriv: v * a.ln() * b.deriv,
        }
    }

    pub fn powi(self, n: i32) -> Self {
        if n == 0 {
            return Self::constant(1.0);
        }
        let v = self.value.powi(n);
        let d = (n as f64) * self.value.powi(n - 1) * self.deriv;
        Self { value: v, deriv: d }
    }

    pub fn sin(self) -> Self {
        Self {
            value: self.value.sin(),
            deriv: self.value.cos() * self.deriv,
        }
    }

    pub fn cos(self) -> Self {
        Self {
            value: self.value.cos(),
            deriv: -self.value.sin() * self.deriv,
        }
    }

    pub fn tan(self) -> Self {
        Self {
            value: self.value.tan(),
            deriv: self.deriv / (self.value.cos() * self.value.cos()),
        }
    }

    pub fn exp(self) -> Self {
        let v = self.value.exp();
        Self {
            value: v,
            deriv: v * self.deriv,
        }
    }

    pub fn ln(self) -> Self {
        Self {
            value: self.value.ln(),
            deriv: self.deriv / self.value,
        }
    }

    pub fn sqrt(self) -> Self {
        let v = self.value.sqrt();
        Self {
            value: v,
            deriv: self.deriv / (2.0 * v),
        }
    }

    pub fn sinh(self) -> Self {
        Self {
            value: self.value.sinh(),
            deriv: self.value.cosh() * self.deriv,
        }
    }

    pub fn cosh(self) -> Self {
        Self {
            value: self.value.cosh(),
            deriv: self.value.sinh() * self.deriv,
        }
    }

    pub fn tanh(self) -> Self {
        let t = self.value.tanh();
        Self {
            value: t,
            deriv: (1.0 - t * t) * self.deriv,
        }
    }
}

impl Neg for Dual<f64> {
    type Output = Self;
    fn neg(self) -> Self::Output {
        Self {
            value: -self.value,
            deriv: -self.deriv,
        }
    }
}

macro_rules! impl_op {
    ($trait:ident, $method:ident, $op_dual_dual:expr, $op_dual_f:expr, $op_f_dual:expr) => {
        impl $trait<Dual<f64>> for Dual<f64> {
            type Output = Dual<f64>;
            fn $method(self, rhs: Dual<f64>) -> Self::Output {
                $op_dual_dual(self, rhs)
            }
        }

        impl $trait<f64> for Dual<f64> {
            type Output = Dual<f64>;
            fn $method(self, rhs: f64) -> Self::Output {
                $op_dual_f(self, rhs)
            }
        }

        impl $trait<Dual<f64>> for f64 {
            type Output = Dual<f64>;
            fn $method(self, rhs: Dual<f64>) -> Self::Output {
                $op_f_dual(self, rhs)
            }
        }
    };
}

impl_op!(
    Add,
    add,
    |a: Dual<f64>, b: Dual<f64>| Dual::new(a.value + b.value, a.deriv + b.deriv),
    |a: Dual<f64>, b: f64| Dual::new(a.value + b, a.deriv),
    |a: f64, b: Dual<f64>| Dual::new(a + b.value, b.deriv)
);

impl_op!(
    Sub,
    sub,
    |a: Dual<f64>, b: Dual<f64>| Dual::new(a.value - b.value, a.deriv - b.deriv),
    |a: Dual<f64>, b: f64| Dual::new(a.value - b, a.deriv),
    |a: f64, b: Dual<f64>| Dual::new(a - b.value, -b.deriv)
);

impl_op!(
    Mul,
    mul,
    |a: Dual<f64>, b: Dual<f64>| Dual::new(
        a.value * b.value,
        a.deriv * b.value + a.value * b.deriv
    ),
    |a: Dual<f64>, b: f64| Dual::new(a.value * b, a.deriv * b),
    |a: f64, b: Dual<f64>| Dual::new(a * b.value, a * b.deriv)
);

impl_op!(
    Div,
    div,
    |a: Dual<f64>, b: Dual<f64>| Dual::new(
        a.value / b.value,
        (a.deriv * b.value - a.value * b.deriv) / (b.value * b.value)
    ),
    |a: Dual<f64>, b: f64| Dual::new(a.value / b, a.deriv / b),
    |a: f64, b: Dual<f64>| Dual::new(a / b.value, (-a * b.deriv) / (b.value * b.value))
);

pub fn diff<F>(f: F, x: f64) -> f64
where
    F: Fn(Dual<f64>) -> Dual<f64>,
{
    let res = f(Dual::variable(x));
    res.deriv
}

// ------------------------------------------------------------------------------------------------
// Numeric Context & Generic Traits
// ------------------------------------------------------------------------------------------------

pub trait Numeric:
    Sized
    + Clone
    + Copy
    + Add<Self, Output = Self>
    + Sub<Self, Output = Self>
    + Mul<Self, Output = Self>
    + Div<Self, Output = Self>
{
    fn from_f64(v: f64) -> Self;
    fn to_f64(self) -> f64;
    fn ln(self) -> Self;
    fn powi(self, n: i32) -> Self;
}

impl Numeric for f64 {
    fn from_f64(v: f64) -> Self {
        v
    }
    fn to_f64(self) -> f64 {
        self
    }
    fn ln(self) -> Self {
        f64::ln(self)
    }
    fn powi(self, n: i32) -> Self {
        f64::powi(self, n)
    }
}

impl Numeric for Dual<f64> {
    fn from_f64(v: f64) -> Self {
        Dual::constant(v)
    }
    fn to_f64(self) -> f64 {
        self.value
    }
    fn ln(self) -> Self {
        self.ln()
    }
    fn powi(self, n: i32) -> Self {
        self.powi(n)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn assert_diff(f: impl Fn(Dual<f64>) -> Dual<f64>, df_analytical: impl Fn(f64) -> f64, x: f64) {
        let expected = df_analytical(x);
        let actual = diff(f, x);
        let diff = (expected - actual).abs();
        assert!(
            diff < 1e-9,
            "x={} | expected={}, actual={}, diff={}",
            x,
            expected,
            actual,
            diff
        );
    }

    #[test]
    fn test_unary_minus() {
        assert_diff(|x| -x, |_| -1.0, 2.0);
    }

    #[test]
    fn test_arithmetic() {
        let x0 = 2.0;
        assert_diff(|x| x + x, |_| 2.0, x0);
        assert_diff(|x| x + 3.0, |_| 1.0, x0);
        assert_diff(|x| 3.0 + x, |_| 1.0, x0);

        assert_diff(|x| x - x, |_| 0.0, x0);
        assert_diff(|x| x - 3.0, |_| 1.0, x0);
        assert_diff(|x| 3.0 - x, |_| -1.0, x0);

        assert_diff(|x| x * x, |x| 2.0 * x, x0);
        assert_diff(|x| x * 3.0, |_| 3.0, x0);
        assert_diff(|x| 3.0 * x, |_| 3.0, x0);

        assert_diff(|x| x / x, |_| 0.0, x0);
        assert_diff(|x| x / 3.0, |_| 1.0 / 3.0, x0);
        assert_diff(|x| 3.0 / x, |x| -3.0 / (x * x), x0);
    }

    #[test]
    fn test_power() {
        let x0 = 2.0;
        assert_diff(|x| x.pow(x), |x| (x.powf(x)) * (x.ln() + 1.0), x0);
        assert_diff(|x| x.powf(3.0), |x| 3.0 * (x.powf(2.0)), x0);
        assert_diff(
            |x| Dual::f_pow(3.0, x),
            |x| (3.0_f64.powf(x)) * 3.0_f64.ln(),
            x0,
        );
    }

    #[test]
    fn test_math_functions() {
        let x0 = 2.0;
        assert_diff(|x| x.sin(), |x| x.cos(), x0);
        assert_diff(|x| x.cos(), |x| -x.sin(), x0);
        assert_diff(|x| x.tan(), |x| 1.0 / (x.cos().powi(2)), x0);
        assert_diff(|x| x.exp(), |x| x.exp(), x0);
        assert_diff(|x| x.ln(), |x| 1.0 / x, x0);
        assert_diff(|x| x.sqrt(), |x| 0.5 / x.sqrt(), x0);
        assert_diff(|x| x.sinh(), |x| x.cosh(), x0);
        assert_diff(|x| x.cosh(), |x| x.sinh(), x0);
        assert_diff(|x| x.tanh(), |x| 1.0 - x.tanh().powi(2), x0);
    }

    #[test]
    fn test_calphad_example() {
        let g =
            |t: Dual<f64>| Dual::constant(10.0) + t * 2.5 - t * 3.0 * t.ln() + t.powf(2.0) * 0.5;
        let dg_analytical = |t: f64| 2.5 - 3.0 * (t.ln() + 1.0) + t;
        assert_diff(g, dg_analytical, 300.0);
    }
}
