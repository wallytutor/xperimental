use crate::thermo::Substance;
use std::collections::HashMap;

pub fn get_calcite() -> Substance {
    Substance {
        molar_mass: 100.087,
        molar_volume: 36.934,
        delta_gf: -1129109.0,
        delta_hf: -1207605.0,
        s0: 91.780,
        cp: 83.47,
        raw_coefs: vec![99.72, 0.02692, -2158000.0],
        elements: HashMap::from([
            ("Ca".to_string(), 1.0),
            ("C".to_string(), 1.0),
            ("O".to_string(), 3.0),
        ]),
    }
}

pub fn get_lime() -> Substance {
    Substance {
        molar_mass: 56.077,
        molar_volume: 16.760,
        delta_gf: -603296.0,
        delta_hf: -634920.0,
        s0: 38.100,
        cp: 42.05,
        raw_coefs: vec![51.86, 0.00244, -937000.0],
        elements: HashMap::from([("Ca".to_string(), 1.0), ("O".to_string(), 1.0)]),
    }
}

pub fn get_co2() -> Substance {
    Substance {
        molar_mass: 44.010,
        molar_volume: 25.300,
        delta_gf: -394373.0,
        delta_hf: -393510.0,
        s0: 213.785,
        cp: 37.14,
        raw_coefs: vec![33.98, 0.02388, -352000.0],
        elements: HashMap::from([("C".to_string(), 1.0), ("O".to_string(), 2.0)]),
    }
}
