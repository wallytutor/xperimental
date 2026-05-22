# Makefile for WSL/Linux
CC = gcc
CFLAGS = -Wall -Wextra -O2 -fPIC -ansi -pedantic -I../src
LDFLAGS = -shared

TARGET_LIB = libwsgglib.so
TARGET_APP = wsggapp

SRCS = wsgglib.c
OBJS = $(SRCS:.c=.o)

all: $(TARGET_LIB) $(TARGET_APP)

$(TARGET_LIB): $(OBJS)
	$(CC) $(LDFLAGS) -o $@ $^ -lm

$(TARGET_APP): wsggapp.c $(TARGET_LIB)
	$(CC) $(CFLAGS) -o $@ wsggapp.c -L. -lwsgglib -lm -Wl,-rpath,.

%.o: ../src/%.c
	$(CC) $(CFLAGS) -c $< -o $@

clean:
	rm -f $(OBJS) $(TARGET_LIB) $(TARGET_APP)

.PHONY: all clean
