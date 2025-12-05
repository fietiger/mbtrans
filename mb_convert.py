import csv

def read_pairs(path):
    for enc in ('utf-8', 'utf-8-sig', 'gb18030'):
        try:
            with open(path, 'r', encoding=enc, newline='') as f:
                reader = csv.reader(f)
                header = next(reader, None)
                if not header:
                    return []
                i_code = header.index('编码')
                i_word = header.index('词条')
                lines = []
                for row in reader:
                    if len(row) <= max(i_code, i_word):
                        continue
                    code = row[i_code].strip()
                    word = row[i_word].strip()
                    lines.append(f'{code} {word}')
                return lines
        except UnicodeDecodeError:
            continue
    return []

def main():
    input_paths = [
        'd:/Dev2/mbtran/cqyx/主码-系统码表.csv',
        'd:/Dev2/mbtran/cqyx/单字码表.csv',
    ]
    out_path = 'mb.txt'
    with open(out_path, 'w', encoding='utf-8') as out:
        for p in input_paths:
            for line in read_pairs(p):
                out.write(line + '\n')

if __name__ == '__main__':
    main()

