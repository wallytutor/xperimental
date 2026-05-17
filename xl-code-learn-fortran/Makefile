FC           = /usr/bin/gfortran
FCFLAGS      = -O2 -Wall -Jbin

SRC_DIR      = src
BIN_DIR      = bin

QUICK_DIR   = $(SRC_DIR)/

SOURCES_QUICK      = $(wildcard $(QUICK_DIR)/*.f90)
EXECUTABLES_QUICK  = $(SOURCES_QUICK:.f90=.out)
EXECUTABLES_QUICK := $(patsubst $(QUICK_DIR)/%, $(BIN_DIR)/%, $(EXECUTABLES_QUICK))

all: $(BIN_DIR) $(EXECUTABLES_QUICK)

$(BIN_DIR):
	mkdir -p $(BIN_DIR)

$(BIN_DIR)/%.out: $(QUICK_DIR)/%.f90
	$(FC) $(FCFLAGS) -o $@ $<

clean:
	rm -rf $(BIN_DIR)
