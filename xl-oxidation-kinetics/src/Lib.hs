module Lib ( modelSolver ) where

-- ######################################################################## --
-- Type aliases
-- ######################################################################## --

-- Numeric type alias.
type Nb = Double

-- Triple type alias.
type Triple = (Nb, Nb, Nb)

-- ######################################################################## --
-- Thermophysical properties
-- ######################################################################## --

-- Temperature and thickness dependent emissivity model.
eps :: Nb -> Nb -> Nb -> Nb -> Nb
eps t tk tc tm = p * epsSteel + (1 - p) * epsOxide
    where p = exp (-(t / tc) ** tm)
          epsSteel = epsModel tk epsParsSteel
          epsOxide = epsModel tk epsParsOxide
          epsModel beta (a, b) = a * (1 - exp (-beta / b))

-- Steel heat capacity [J/(kg.K)].
cp :: Nb -> Nb
cp tk = tk * (tk * (tk * (e * tk + d) + c) + b) + a
    where (a, b, c, d, e) = if tk < 1123 then cpParsL else cpParsH

-- Oxide emissivity parameters for temperature dependency.
epsParsOxide :: (Nb, Nb)
epsParsOxide = (9.35083100e-01, 5.1228971691e+02)

-- Steel emissivity parameters for temperature dependency.
epsParsSteel :: (Nb, Nb)
epsParsSteel = (3.08938352e-01, 8.4004408700e+02)

-- Low temperature range heat capacity coefficients.
cpParsL :: (Nb, Nb, Nb, Nb, Nb)
cpParsL = (7.726812135693e+02, -3.061763527505e+00,
           9.713590095324e-03, -1.177081491234e-05,
           5.368288667701e-09)

-- High temperature range heat capacity coefficients.
cpParsH :: (Nb, Nb, Nb, Nb, Nb)
cpParsH = (5.830451419897e+02, -1.943420512064e-01,
           2.581569711817e-04, -6.320141833273e-08,
           1.932134676071e-13)

-- ######################################################################## --
-- Model implementation
-- ######################################################################## --

-- Right-hand side of system of ODE's.
rhs :: Nb -> Nb -> Nb -> Nb -> Nb -> Nb -> Nb -> (Nb, Nb)
rhs t tk tw l h tc tm = (tmpDot t tk tw l h tc tm, tauDot t tk)

-- Time derivative of strip temperature.
tmpDot :: Nb -> Nb -> Nb -> Nb -> Nb -> Nb -> Nb -> Nb
tmpDot t tk tw l h tc tm = 2 * (rad + con) / (rhoSteel * l * cp tk)
    where rad   = sigma * (alpha * tw**4.0 - gamma * tk**4.0)
          con   = h * (tw - tk)
          alpha = eps t tw tc tm
          gamma = eps t tk tc tm

-- Time derivative of oxide thickness.
tauDot :: Nb -> Nb -> Nb
tauDot t tk = (kModel / t) * exp (-actEnergy / (gasConstant * tk))

-- Problem constant coefficient [m^2/s].
kModel :: Nb
kModel = (rhoSteel / rhoOxide) * (mwOxide / mwSteel) * diffZero

-- Stefan-Boltzmann constant [W/(m^2.K^4)].
sigma :: Nb
sigma = 5.670374419e-08

-- Ideal gas constant [J/(mol.K)].
gasConstant :: Nb
gasConstant = 8.31446261815324

-- Activation energy for diffusion in oxide [J/(mol.K)].
actEnergy :: Nb
actEnergy =  230000

-- Specific mass of magnetite [kg/m^3].
rhoOxide :: Nb
rhoOxide = 7874

-- Specific mass of steel [kg/m^3].
rhoSteel :: Nb
rhoSteel = 7890

-- Molar mass of magnetite [kg/mol].
mwOxide :: Nb
mwOxide = 0.231533

-- Molar mass of steel [kg/mol].
mwSteel :: Nb
mwSteel = 0.055845

-- Pre-exponential diffusivity coefficient [m^2/s].
diffZero :: Nb
diffZero = 0.00052

-- ######################################################################## --
-- Integrator
-- ######################################################################## --

-- Basic Euler integrator (to be replaced by RK4).
euler :: (Triple -> (Nb, Nb)) -> Triple -> Nb -> Nb -> [Triple]
euler f (y0, t0, tk0) dy yf = triples
    where iterator = iterate $ eulerStep f dy
          triples  = takeWhile (\(y, _t, _tk) -> y <= yf) $ iterator (y0, t0, tk0)
          eulerStep fn d (y, t, tk) = (ynew, tnew, tknew)
              where ynew  = y + d
                    tnew  = t + d * dt
                    tknew = tk + d * dtk
                    (dtk, dt) = fn (y, t, tk)

-- ######################################################################## --
-- Main solver function
-- ######################################################################## --

modelSolver :: Nb -> Nb -> Nb -> Nb -> Nb -> Nb -> Nb -> Nb -> Nb -> IO ()
modelSolver t tk tw l h tc tm yf dy = do
    putStrLn "\n*** WELCOME TO OXYL ***"
    putStrLn ("Initial oxide thickness ....... [nm] " ++ show t)
    putStrLn ("Initial steel temperature ...... [K] " ++ show tk)
    putStrLn ("Furnace wall temperature ....... [K] " ++ show tw)
    putStrLn ("Steel strip thickness .......... [m] " ++ show l)
    putStrLn ("Convection coefficient ... [W/(m.K)] " ++ show h)
    putStrLn ("Oxydation half thickness ...... [nm] " ++ show tc)
    putStrLn ("Oxydation exponent ............. [-] " ++ show tm)
    putStrLn ("End integration time ........... [s] " ++ show yf)
    putStrLn ("Integration time step .......... [s] " ++ show dy)

    let ode (y, t0, tk0) = rhs t0 tk0 tw l h tc tm
    let sol = euler ode (0, t, tk) dy yf

    writeFile "results.csv" (toCsv sol ',')
    putStrLn "*** GOOD-BYE ***\n"

-- Prepare results for dumping as CSV file.
toCsv :: Show a => [(a, a, a)] -> Char -> String
toCsv solution sep = (unlines . csvTab) solution
    where
        listToString   = unwords . map show
        newLine xs     = [if x == ' ' then sep else x | x<-xs]
        csvTab triples = [newLine $ listToString [x, y, z] | (x, y, z) <- triples]