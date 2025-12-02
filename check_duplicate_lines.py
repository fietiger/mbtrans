import sys
from collections import defaultdict

def check_duplicate_lines(file_path):
    """
    检查文件中是否有重复的行，并输出重复行及其出现次数
    """
    print(f"开始检查文件: {file_path}")
    
    # 使用字典来统计每行出现的次数
    line_counts = defaultdict(int)
    line_number = 0
    duplicates_found = False
    
    try:
        # 逐行读取文件以避免内存问题
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            for line in f:
                line_number += 1
                # 去除行尾的换行符和空白字符
                stripped_line = line.rstrip('\r\n')
                # 跳过空行
                if stripped_line:
                    line_counts[stripped_line] += 1
                    
                # 每处理10000行显示进度
                if line_number % 10000 == 0:
                    print(f"已处理 {line_number} 行...")
        
        print(f"文件读取完成，共处理 {line_number} 行")
        print("\n查找重复行中...")
        
        # 找出并显示重复的行
        duplicate_lines = {line: count for line, count in line_counts.items() if count > 1}
        
        if duplicate_lines:
            duplicates_found = True
            print(f"\n找到 {len(duplicate_lines)} 个重复行：")
            
            # 按出现次数排序并显示前20个重复行
            sorted_duplicates = sorted(duplicate_lines.items(), key=lambda x: x[1], reverse=True)
            for i, (line, count) in enumerate(sorted_duplicates[:20], 1):
                # 限制显示的行长度
                display_line = line[:100] + '...' if len(line) > 100 else line
                print(f"{i}. 出现 {count} 次: {display_line}")
            
            if len(sorted_duplicates) > 20:
                print(f"\n... 还有 {len(sorted_duplicates) - 20} 个重复行未显示")
        else:
            print("\n未找到重复行")
            
        return duplicates_found
        
    except FileNotFoundError:
        print(f"错误: 找不到文件 '{file_path}'")
        return False
    except Exception as e:
        print(f"处理文件时出错: {str(e)}")
        return False

if __name__ == "__main__":
    # 支持命令行参数，默认为新生成的文件
    import sys
    if len(sys.argv) > 1:
        file_path = sys.argv[1]
    else:
        file_path = "d:\\Dev2\\mbtran\\def3_output_new.txt"
    check_duplicate_lines(file_path)