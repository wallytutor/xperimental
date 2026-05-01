// Diffusion1DLinear.cs
// Author: Walter Dal'Maz Silva
// Date: August 5nd 2019
// Modified: May 1st 2026

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Diffusion
{
    /// <summary>
    /// Input structure for <c>SolverLinear</c>.
    /// </summary>
    public struct ProblemData
    {
        /// <summary>
        /// Number of nodes in domain.
        /// </summary>
        public int nodes;

        /// <summary>
        /// Thickness of plate given in meters.
        /// </summary>
        public double length;

        /// <summary>
        /// Diffusion coefficient provided in square meters per second.
        /// </summary>
        public double Dc;

        /// <summary>
        /// Mass transfer coefficient in west boundary in meters per second.
        /// </summary>
        public double hw;

        /// <summary>
        /// Mass transfer coefficient in east boundary in meters per second.
        /// </summary>
        public double he;

        /// <summary>
        /// Species concentration in west environment.
        /// </summary>
        public double cw;

        /// <summary>
        /// Species concentration in east environment.
        /// </summary>
        public double ce;
    }

    /// <summary>
    /// Class <c>SolverLinear</c> models the integration of linear diffusion
    /// equation in one dimension using a upwind scheme for spacial derivatives
    /// and an implicit solution for time-stepping.
    /// </summary>
    public class SolverLinear
    {
        private ProblemData m_data;

        private double m_dx;
        private double m_dt;

        private List<double> m_arr;
        private List<double> m_d;
        private List<double> m_u;
        private List<double> m_l;

        private double m_beta;
        private double m_gammaw;
        private double m_gammae;

        /// <summary>
        /// Solver construction from array of initial state.
        /// </summary>
        /// <param name="data">Problem coefficients.</param>
        /// <param name="arr">Array of initial state.</param>
        public SolverLinear(ProblemData data, List<double> arr)
        {
            Console.WriteLine("> Getting parameters...");
            m_data = data;

            Console.WriteLine("> Setting initial state...");
            m_arr = DiffusionExtensions.Clone(arr);
            m_d = new List<double>(m_arr.Count);
            m_u = new List<double>(m_arr.Count - 1);
            m_l = new List<double>(m_arr.Count - 1);
        }

        /// <summary>
        /// Solver construction with constant initial state.
        /// </summary>
        /// <param name="data">Problem coefficients.</param>
        /// <param name="arr">Constant initial state.</param>
        public SolverLinear(ProblemData data, double c0)
        {
            Console.WriteLine("> Getting parameters...");
            m_data = data;

            Console.WriteLine("> Setting initial state...");
            m_arr = Enumerable.Repeat(c0, m_data.nodes).ToList();
            m_d = new List<double>(m_arr.Count);
            m_u = new List<double>(m_arr.Count - 1);
            m_l = new List<double>(m_arr.Count - 1);
        }

        /// <summary>
        /// Advance solution in time with a given number of steps.
        /// </summary>
        /// <param name="time">Total time to advance in seconds.</param>
        /// <param name="steps">Number of internal steps to perform.</param>
        /// <param name="save">If true, save data of all steps.</param>
        public void Advance(double time, uint steps, bool save)
        {
            // Compute space and time steps.
            m_dx = m_data.length / m_data.nodes;
            m_dt = time / steps;

            // Compute problem coefficients.
            m_beta = m_data.Dc * m_dt / (m_dx * m_dx);
            m_gammaw = -m_data.hw * m_dt / m_dx;
            m_gammae = -m_data.he * m_dt / m_dx;

            // Assembly tridiagonal matrix.
            m_d = Enumerable.Repeat(1 + 2.0 * m_beta, m_data.nodes).ToList();
            m_u = Enumerable.Repeat(-m_beta, m_data.nodes - 1).ToList();
            m_l = Enumerable.Repeat(-m_beta, m_data.nodes - 1).ToList();

            // Modify boundary conditions.
            m_d[0] = 1 + m_beta - m_gammae;
            m_d[m_data.nodes - 1] = 1 + m_beta - m_gammaw;

            Console.WriteLine("> Advancing solution in time...");
            Console.WriteLine($"> Using a time step of {m_dt} s");

            for (int i = 0; i != steps; i++)
            {
                // Apply boundary fluxes to RHS.
                m_arr[0] -= m_gammae * m_data.ce;
                m_arr[m_data.nodes - 1] -= m_gammaw * m_data.cw;

                // Get output time.
                if (i % 100 == 0)
                {
                    string instant = ((i + 1) * m_dt).ToString("e6");
                    Console.WriteLine($"> Advancing to {instant} s");
                }

                // Save results if required.
                //if (save)
                //{
                //
                //}

                // Solve one time-step (result in m_arr)
                DiffusionExtensions.SolveTDMA(m_l, m_d, m_u, m_arr);
            }
        }

        /// <summary>
        /// Retrieve current state of the problem.
        /// </summary>
        /// <param name="arr">Array of concentrations over domain.</param>
        public void GetState(out List<double> arr)
        {
            arr = DiffusionExtensions.Clone(m_arr);
        }

        /// <summary>
        /// Save results of a given state to a CSV file.
        /// </summary>
        /// <param name="csv_file">Path to output file.</param>
        public void ToCSV(string csv_file)
        {
            // Create a buffer for results.
            StringBuilder csv = new StringBuilder();

            // Feed the buffer row by row.
            for (int i = 0; i != m_data.nodes; i++)
            {
                csv.AppendLine($"{i * m_dx},{m_arr[i]}");
            }

            // (Over-)Write string buffer to file.
            File.WriteAllText(csv_file, csv.ToString());
        }
    }

    /// <summary>
    /// Static class <c>DiffusionExtensions</c> provides auxiliary functions
    /// for the solution of diffusion equation in specialized solvers. This
    /// class should be compiled as its own DLL in future versions given its
    /// generic functionalities.
    /// </summary>
    static class DiffusionExtensions
    {
        /// <summary>
        /// Provides a clonning functionality for a list of doubles.
        /// </summary>
        /// <param name="listToClone">The list to be clonned.</param>
        /// <returns>A copy of parameter list.</returns>
        public static List<double> Clone(this List<double> listToClone)
        {
            if (!listToClone.Any())
            {
                throw new InvalidOperationException("Empty list to clone");
            }
            return listToClone.Select(item => item).ToList();
        }

        /// <summary>
        /// Solve a tridiagonal system with Thomas algorithm. Only the diagonals
        /// of the problem matrix are provided for sparse storage.
        /// </summary>
        /// <param name="L">Matrix lower diagonal.</param>
        /// <param name="D">Matrix main diagonal.</param>
        /// <param name="U">Matrix upper diagonal.</param>
        /// <param name="S">Problem right-hand side.</param>
        public static void SolveTDMA(List<double> L, List<double> D,
            List<double> U, List<double> S)
        {
            if ((L.Count != U.Count) || (D.Count != S.Count) || (D.Count != U.Count + 1))
            {
                throw new ArrayTypeMismatchException("Mismatching sizes in TDMA");
            }

            // Get main problem size.
            int N = D.Count;

            // Clone arrays.
            List<double> A = Clone(L);
            List<double> B = Clone(D);
            List<double> C = Clone(U);

            // Forward diagonalization.
            for (int i = 1; i < N; i++)
            {
                double m = A[i - 1] / B[i - 1];
                B[i] -= m * C[i - 1];
                S[i] -= m * S[i - 1];
            }

            // Solve for last element.
            S[N - 1] /= B[N - 1];

            // Back-substitution for other elements.
            for (int i = N - 2; i >= 0; i--)
            {
                S[i] = (S[i] - C[i] * S[i + 1]) / B[i];
            }
        }
    }
}
