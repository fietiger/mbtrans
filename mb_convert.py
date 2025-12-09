import csv
from collections import defaultdict, OrderedDict

def read_pairs(path):
    """读取单个CSV文件，返回按编码分组的字典，键为编码，值为列表[(排序值, 词条), ...]"""
    for enc in ('utf-8', 'utf-8-sig', 'gb18030'):
        try:
            with open(path, 'r', encoding=enc, newline='') as f:
                reader = csv.reader(f)
                header = next(reader, None)
                if not header:
                    return defaultdict(list)
                
                # 获取各字段的索引
                i_code = header.index('编码')
                i_word = header.index('词条')
                i_sort = header.index('候选排序')
                
                # 使用字典按编码分组，每个编码对应一个列表，包含(候选排序, 词条)元组
                code_dict = defaultdict(list)
                
                for row in reader:
                    if len(row) <= max(i_code, i_word, i_sort):
                        continue
                    
                    code = row[i_code].strip()
                    word = row[i_word].strip()
                    
                    # 解析候选排序，转换为整数
                    try:
                        sort_value = int(row[i_sort].strip())
                    except ValueError:
                        # 如果候选排序无法转换为整数，默认使用0
                        sort_value = 0
                    
                    code_dict[code].append((sort_value, word))
                
                return code_dict
        except UnicodeDecodeError:
            continue
    return defaultdict(list)

def main():
    input_paths = [
        'd:/Dev2/mbtran/cqyx/主码-系统码表.csv',
        'd:/Dev2/mbtran/cqyx/单字码表.csv',
    ]
    out_path = 'mb.txt'
    
    # 1. 读取所有文件的内容，按文件顺序和编码分组
    all_data = defaultdict(list)  # key: 编码, value: list of [(文件索引, 排序值, 词条), ...]
    
    for file_index, file_path in enumerate(input_paths):
        file_data = read_pairs(file_path)
        for code, items in file_data.items():
            for sort_value, word in items:
                all_data[code].append((file_index, sort_value, word))
    
    # 2. 处理数据，生成输出行
    lines = []
    
    # 对所有编码按字母顺序排序
    all_codes = sorted(all_data.keys())
    
    # 遍历所有按字母顺序排序的编码
    for code in all_codes:
        items = all_data[code]
        # 按文件索引升序排序（先处理前面文件的数据）
        # 同一文件内的相同编码条目，按排序值降序排序
        sorted_items = sorted(items, key=lambda x: (x[0], -x[1]))
        
        # 生成输出行
        for _, _, word in sorted_items:
            lines.append(f'{code} {word}')
    
    # 3. 写入输出文件
    with open(out_path, 'w', encoding='utf-8') as out:
        for line in lines:
            out.write(line + '\n')
    
    print(f"转换完成，共处理 {len(lines)} 条记录")
    print(f"输出文件：{out_path}")

if __name__ == '__main__':
    main()

