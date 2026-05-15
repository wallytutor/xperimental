use crate::thermo::{AggregationType, Substance};
use std::collections::HashMap;

pub fn get_calcite() -> Substance {
    Substance {
        molar_mass: 100.087,
        molar_volume: 36.934,
        delta_gf: -1129109.0,
        delta_hf: -1207605.0,
        s0: 91.780,
        raw_coefs: vec![99.72, 0.02692, -2158000.0],
        elements: HashMap::from([
            ("Ca".to_string(), 1.0),
            ("C".to_string(), 1.0),
            ("O".to_string(), 3.0),
        ]),
        reference: "".to_string(),
        aggregation_type: AggregationType::Solid,
    }
}

pub fn get_lime() -> Substance {
    Substance {
        molar_mass: 56.077,
        molar_volume: 16.760,
        delta_gf: -603296.0,
        delta_hf: -634920.0,
        s0: 38.100,
        raw_coefs: vec![51.86, 0.00244, -937000.0],
        elements: HashMap::from([("Ca".to_string(), 1.0), ("O".to_string(), 1.0)]),
        reference: "".to_string(),
        aggregation_type: AggregationType::Solid,
    }
}

pub fn get_diaspore() -> Substance {
    Substance {
        molar_mass: 59.988,
        molar_volume: 17.760,
        delta_gf: -922740.0,
        delta_hf: -1001300.0,
        s0: 35.300,
        // raw_coefs: vec![53.33, 0.0, 0.0],
        raw_coefs: vec![49.809839326625806, 0.05858016915762718, -1243143.0926866678],
        elements: HashMap::from([
            ("Al".to_string(), 1.0),
            ("H".to_string(), 1.0),
            ("O".to_string(), 2.0),
        ]),
        reference: "".to_string(),
        aggregation_type: AggregationType::Solid,
    }
}

pub fn get_al2o3() -> Substance {
    Substance {
        molar_mass: 101.96,
        molar_volume: 25.575,
        delta_gf: -1582291.0,
        delta_hf: -1675711.0,
        s0: 50.917,
        raw_coefs: vec![108.74484689911188, 0.02076934906808082, -3319706.5166596076],
        elements: HashMap::from([("Al".to_string(), 2.0), ("O".to_string(), 3.0)]),
        reference: "".to_string(),
        aggregation_type: AggregationType::Solid,
    }
}

pub fn get_co2() -> Substance {
    Substance {
        molar_mass: 44.0098,
        molar_volume: 25.300,
        delta_gf: -394373.0,
        delta_hf: -393510.0,
        s0: 213.676,
        raw_coefs: vec![33.98, 0.02388, -352000.0],
        elements: HashMap::from([("C".to_string(), 1.0), ("O".to_string(), 2.0)]),
        reference: "".to_string(),
        aggregation_type: AggregationType::Gas,
    }
}

pub fn get_h2o() -> Substance {
    Substance {
        molar_mass: 18.0153,
        molar_volume: 18.070,
        delta_gf: -228583.0,
        delta_hf: -241826.0,
        s0: 188.726,
        raw_coefs: vec![27.60, 0.01369, -172000.0],
        elements: HashMap::from([("H".to_string(), 2.0), ("O".to_string(), 1.0)]),
        reference: "".to_string(),
        aggregation_type: AggregationType::Gas,
    }
}
