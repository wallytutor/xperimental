# Nitriding solver in Rust

Solution of diffusion equation in Rust. To run it simply execute `cargo run`.

Output should be something like this:

```
*** NITRIDING MASS INTAKE MODEL ***


* Using B.C. DirichletDirichlet
* The following concerns the full exposed length
* Mass intake by material 3.24 kg/h
* Simulation took is: 190.921184ms


* Using B.C. DirichletSymmetry
* The following concerns the full exposed length
* Mass intake by material 3.24 kg/h
* Simulation took is: 112.42829ms


*** NITRIDING MASS INTAKE MODEL ***
```

Please notice that currently there is no parser for inputs. If you need to simulate with other conditions consider editing `main`, the inputs are reasoably documented with comments for now.

## TODO

- [ ] Migrate reusable code to main library.
- [ ] Implement final state plotting.
- [ ] Implement parameter parsing from input file (JSON/YAML,...).
- [ ] Provide phase transformation (BCC > FCC) during process.
- [ ] Create a library with reusable code for diffusion.
- [ ] Provide a 2-D solver for border effects investigation.
- [ ] [Implement Python API](https://saidvandeklundert.net/learn/2021-11-18-calling-rust-from-python-using-pyo3/)

## Classifiers

#programming/rust #programming/cfd #physics/transport
