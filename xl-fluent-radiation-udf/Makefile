# Makefile for WSL/Linux
CC = gcc
CFLAGS = -Wall -Wextra -O2 -fPIC -ansi -pedantic
LDFLAGS = -shared

TARGET_LIB = libwsgg_radlib.so
TARGET_APP = wsgg_app

SRCS = src/wsgg_radlib_bordbar_2020.c
OBJS = $(SRCS:.c=.o)

all: $(TARGET_LIB) $(TARGET_APP)

$(TARGET_LIB): $(OBJS)
	$(CC) $(LDFLAGS) -o $@ $^ -lm

$(TARGET_APP): src/main.c $(TARGET_LIB)
	$(CC) $(CFLAGS) -o $@ src/main.c -L. -lwsgg_radlib -lm -Wl,-rpath,.

%.o: %.c
	$(CC) $(CFLAGS) -c $< -o $@

clean:
	rm -f $(OBJS) $(TARGET_LIB) $(TARGET_APP)

.PHONY: all clean
