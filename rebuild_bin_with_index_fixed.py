#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import struct
from collections import defaultdict

def get_first_letter(text):
    """获取文本的第一个字符"""
    if text and len(text) > 0:
        return text[0]
    return ''

def rebuild_bin_file_with_index(input_txt_file, output_bin_file):
    """
    根据文本文件重建bin文件，并在文件开头维护索引表
    格式：
    - 第1个字节：0x00
    - 接下来的26*4=104个字节：索引表，记录每个字母开头的数据相对于0x6D偏移量的位置（LE格式）
    - 0x6D之后：实际数据
    - 数据格式：
      - 第一个字节：码的长度M
      - 第二个字节：汉字的字节长度+2（N）
      - M个字母（从a到z）
      - （N-2）个字节的汉字（UTF-16编码）
      - 6个字节的0x00
    """
    # 读取所有数据并按首字母分组
    data_by_letter = defaultdict(list)
    with open(input_txt_file, 'r', encoding='utf-8') as in_f:
        for line in in_f:
            line = line.strip()
            if not line:
                continue
                
            # 分割字母和汉字
            parts = line.split(' ', 1)
            if len(parts) != 2:
                continue
                
            letters = parts[0]
            chinese = parts[1]
            
            # 获取首字母
            first_letter = get_first_letter(letters)
            if first_letter.isalpha() and first_letter.islower():
                data_by_letter[first_letter].append((letters, chinese))
    
    # 创建新的bin文件
    with open(output_bin_file, 'wb') as out_f:
        # 先写入109个字节的占位符（1个字节0x00 + 104个字节索引表 + 3个字节填充）
        out_f.write(b'\x00' * 109)
        
        # 记录每个字母相对于0x6D的偏移量
        offsets = {}
        current_offset = 0  # 相对于0x6D的偏移量，a开头的数据从0开始
        
        # 按字母顺序处理数据
        total_count = 0
        for letter in 'abcdefghijklmnopqrstuvwxyz':
            # 记录当前字母数据的起始偏移量（相对于0x6D）
            offsets[letter] = current_offset
            
            # 写入该字母的所有数据
            if letter in data_by_letter:
                for letters, chinese in data_by_letter[letter]:
                    # 计算长度
                    m = len(letters)  # 码的长度
                    chinese_utf16 = chinese.encode('utf-16-le')  # 汉字转UTF-16 LE
                    n = len(chinese_utf16) + 2  # 汉字的字节长度+2
                    
                    # 写入第一个字节：码的长度M
                    out_f.write(struct.pack('B', m))
                    
                    # 写入第二个字节：汉字的字节长度+2（N）
                    out_f.write(struct.pack('B', n))
                    
                    # 写入M个字母
                    out_f.write(letters.encode('ascii'))
                    
                    # 写入（N-2）个字节的汉字（UTF-16编码）
                    out_f.write(chinese_utf16)
                    
                    # 写入6个字节的0x00
                    out_f.write(b'\x00' * 6)
                    
                    # 更新偏移量和计数
                    current_offset += 1 + 1 + m + len(chinese_utf16) + 6
                    total_count += 1
        
        # 记录文件结束位置
        offsets['end'] = current_offset
        
        # 回到文件开头，写入索引表
        out_f.seek(1)  # 从第2个字节开始写入索引表
        for letter in 'abcdefghijklmnopqrstuvwxyz':
            offset = offsets.get(letter, 0)
            # 以小端格式写入4字节整数
            out_f.write(struct.pack('<I', offset))
        
        print(f"重建完成，共处理了 {total_count} 条记录")
        print("各字母相对于0x6D的偏移量:")
        for letter in 'abcdefghijklmnopqrstuvwxyz':
            print(f"  {letter}: {offsets.get(letter, 0)}")

if __name__ == "__main__":
    input_txt_file = "def3_output.txt"
    output_bin_file = "rebuilt_def3_with_index_fixed.bin"
    rebuild_bin_file_with_index(input_txt_file, output_bin_file)
    print(f"重建的bin文件已保存到 {output_bin_file}")