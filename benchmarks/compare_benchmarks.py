#!/usr/bin/env python3
import sys
import re
import os

THRESHOLD = 0.10  # 10% regression allowed

# Find the latest and previous benchmark result files
def find_benchmark_files():
    files = [f for f in os.listdir('.') if f.startswith('benchmark-results') and f.endswith('.txt')]
    files.sort(reverse=True)
    return files[:2]

def parse_ns_op(filename):
    with open(filename) as f:
        for line in f:
            m = re.search(r'(\w+_\w+)\s+\d+:.*?([0-9.]+) ns/op', line)
            if m:
                yield m.group(1), float(m.group(2))

def main():
    files = find_benchmark_files()
    if len(files) < 2:
        print('Not enough benchmark result files to compare.')
        sys.exit(0)
    prev, curr = files[1], files[0]
    prev_results = dict(parse_ns_op(prev))
    curr_results = dict(parse_ns_op(curr))
    failed = False
    for bench, curr_val in curr_results.items():
        prev_val = prev_results.get(bench)
        if prev_val:
            delta = (curr_val - prev_val) / prev_val
            if delta > THRESHOLD:
                print(f'REGRESSION: {bench}: {prev_val:.2f} ns/op -> {curr_val:.2f} ns/op ({delta*100:.1f}%)')
                failed = True
    if failed:
        sys.exit(1)
    print('No significant regressions detected.')

if __name__ == '__main__':
    main()
