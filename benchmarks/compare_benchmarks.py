#!/usr/bin/env python3
import sys
import os

THRESHOLD = 0.10  # 10% regression allowed

# Find the latest and previous benchmark result files
def find_benchmark_files():
    files = [f for f in os.listdir('.') if f.startswith('benchmark-results') and f.endswith('.txt')]
    files.sort(reverse=True)
    return files[:2]

def parse_time_to_ns(value):
    parts = value.replace(',', '').split()
    if len(parts) < 2:
        return None

    try:
        amount = float(parts[0])
    except ValueError:
        return None

    unit = parts[1].lower()
    if unit == 'ns':
        return amount
    if unit in ('us', 'µs'):
        return amount * 1_000
    if unit == 'ms':
        return amount * 1_000_000
    if unit == 's':
        return amount * 1_000_000_000

    return None

def parse_ns_op(filename):
    method_index = None
    mean_index = None

    with open(filename) as f:
        for line in f:
            if not line.startswith('|'):
                continue

            cells = [cell.strip() for cell in line.strip().strip('|').split('|')]
            if not cells or all(set(cell) <= {'-'} for cell in cells if cell):
                continue

            if 'Method' in cells and 'Mean' in cells:
                method_index = cells.index('Method')
                mean_index = cells.index('Mean')
                continue

            if method_index is None or mean_index is None:
                continue

            if len(cells) <= max(method_index, mean_index):
                continue

            mean_ns = parse_time_to_ns(cells[mean_index])
            if mean_ns is not None:
                yield cells[method_index], mean_ns

def main():
    files = find_benchmark_files()
    if len(files) < 2:
        print('Not enough benchmark result files to compare.')
        sys.exit(0)
    prev, curr = files[1], files[0]
    prev_results = dict(parse_ns_op(prev))
    curr_results = dict(parse_ns_op(curr))
    if not prev_results or not curr_results:
        print('Benchmark result files found, but no BenchmarkDotNet table rows could be parsed.')
        sys.exit(0)

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
