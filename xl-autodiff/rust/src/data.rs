use crate::thermo::{AggregationType, Parameterization, Substance, TemperatureRange};
use std::collections::HashMap;

pub fn get_calcite() -> Substance {
    Substance {
        molar_mass: 100.087,
        molar_volume: 36.934,
        delta_gf: -1129109.0,
        delta_hf: -1207605.0,
        s0: 91.780,
        ranges: vec![TemperatureRange {
            t_min: 298.15,
            t_max: 1200.0,
            model: Parameterization::MaierKelley {
                a: 99.72,
                b: 0.02692,
                c: -2158000.0,
            },
        }],
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
        ranges: vec![TemperatureRange {
            t_min: 298.15,
            t_max: 1200.0,
            model: Parameterization::MaierKelley {
                a: 51.86,
                b: 0.00244,
                c: -937000.0,
            },
        }],
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
        ranges: vec![TemperatureRange {
            t_min: 298.15,
            t_max: 1200.0,
            model: Parameterization::MaierKelley {
                a: 49.809839326625806,
                b: 0.05858016915762718,
                c: -1243143.0926866678,
            },
        }],
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
        ranges: vec![TemperatureRange {
            t_min: 298.15,
            t_max: 1200.0,
            model: Parameterization::MaierKelley {
                a: 108.74484689911188,
                b: 0.02076934906808082,
                c: -3319706.5166596076,
            },
        }],
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
        ranges: vec![
            TemperatureRange {
                t_min: 200.0,
                t_max: 1000.0,
                model: Parameterization::NASA7 {
                    a1: 2.35677352,
                    a2: 8.98459677e-03,
                    a3: -7.12356269e-06,
                    a4: 2.45919022e-09,
                    a5: -1.43699548e-13,
                    a6: -4.83719697e+04,
                    a7: 9.90105222,
                },
            },
            TemperatureRange {
                t_min: 1000.0,
                t_max: 6000.0,
                model: Parameterization::NASA7 {
                    a1: 4.63659493,
                    a2: 2.74131991e-03,
                    a3: -9.95828531e-07,
                    a4: 1.60373011e-10,
                    a5: -9.16103468e-15,
                    a6: -4.90249341e+04,
                    a7: -1.93534855,
                },
            },
        ],
        elements: HashMap::from([("C".to_string(), 1.0), ("O".to_string(), 2.0)]),
        reference: "NASA/Cantera".to_string(),
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
        ranges: vec![
            TemperatureRange {
                t_min: 200.0,
                t_max: 1000.0,
                model: Parameterization::NASA7 {
                    a1: 4.19864056,
                    a2: -2.0364341e-03,
                    a3: 6.52040211e-06,
                    a4: -5.48797062e-09,
                    a5: 1.77197817e-12,
                    a6: -3.02937267e+04,
                    a7: -0.849032208,
                },
            },
            TemperatureRange {
                t_min: 1000.0,
                t_max: 6000.0,
                model: Parameterization::NASA7 {
                    a1: 2.67703787,
                    a2: 2.97318329e-03,
                    a3: -7.7376969e-07,
                    a4: 9.44336689e-11,
                    a5: -4.26900959e-15,
                    a6: -2.98858938e+04,
                    a7: 6.88255571,
                },
            },
        ],
        elements: HashMap::from([("H".to_string(), 2.0), ("O".to_string(), 1.0)]),
        reference: "NASA/Cantera".to_string(),
        aggregation_type: AggregationType::Gas,
    }
}
