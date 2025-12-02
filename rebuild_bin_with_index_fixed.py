#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import struct
import re

def read_input_data(txt_file):
    """读取并解析输入文本文件，去除重复内容（包括行中间有多个空格的情况）"""
    data_list = []
    seen_entries = set()  # 用于跟踪已经处理过的条目，实现去重
    
    with open(txt_file, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            
            # 将连续的空格替换为单个空格，用于标准化行内容
            normalized_line = re.sub(r'\s+', ' ', line)
            
            # 分割标准化后的行
            parts = normalized_line.split(' ', 1)
            if len(parts) != 2:
                continue
            
            letters, chinese = parts
            if letters and letters[0].isalpha() and letters[0].islower():
                # 使用元组(letters, chinese)作为键进行去重
                entry_key = (letters, chinese)
                if entry_key not in seen_entries:
                    seen_entries.add(entry_key)
                    data_list.append(entry_key)
    
    # 按字母码排序
    data_list.sort(key=lambda x: x[0])
    return data_list


def encode_entry(letters, chinese):
    """将一条记录编码为二进制格式"""
    m = len(letters)
    chinese_utf16 = chinese.encode('utf-16-le')
    n = len(chinese_utf16) + 2
    
    # 组装数据：M(1字节) + N(1字节) + 字母(M字节) + 汉字(N-2字节) + 填充(6字节)
    data = struct.pack('B', m)
    data += struct.pack('B', n)
    data += letters.encode('ascii')
    data += chinese_utf16
    data += b'\x00' * 6
    
    return data


def calculate_offsets(data_list):
    """计算每个字母对应的结束偏移量"""
    # 初始化26个字母的偏移量
    offsets = {chr(ord('a') + i): 0 for i in range(26)}
    
    current_offset = 0
    current_letter = None
    
    for letters, chinese in data_list:
        first_letter = letters[0]
        
        # 当遇到新字母时,更新之前字母的结束位置
        if first_letter != current_letter:
            if current_letter is not None:
                # 填充从current_letter到first_letter之间的所有字母
                start_idx = ord(current_letter) - ord('a')
                end_idx = ord(first_letter) - ord('a')
                for i in range(start_idx, end_idx):
                    offsets[chr(ord('a') + i)] = current_offset
            current_letter = first_letter
        
        # 计算这条记录的大小
        entry_size = 2 + len(letters) + len(chinese.encode('utf-16-le')) + 6
        current_offset += entry_size
    
    # 填充最后一个字母到'z'的偏移量
    if current_letter:
        start_idx = ord(current_letter) - ord('a')
        for i in range(start_idx, 26):
            offsets[chr(ord('a') + i)] = current_offset
    
    return offsets, current_offset


def write_bin_file(output_file, data_list, offsets):
    """写入二进制文件"""
    with open(output_file, 'wb') as f:
        # 写入文件头：1字节0x00 + 108字节索引表
        f.write(b'\x00')
        
        # 写入索引表：第一个是0,后面26个是每个字母的结束偏移量
        f.write(struct.pack('<I', 0))
        for letter in 'abcdefghijklmnopqrstuvwxyz':
            f.write(struct.pack('<I', offsets[letter]))
        
        # 此时应该正好是109字节，数据从0x6D (109)开始
        
        # 写入所有数据条目
        for letters, chinese in data_list:
            f.write(encode_entry(letters, chinese))


def rebuild_bin_file_with_index(input_txt_file, output_bin_file):
    """
    根据文本文件重建bin文件,并在文件开头维护索引表
    
    文件格式：
    - 0x00: 1字节标志位(0x00)
    - 0x01-0x6C: 27个4字节整数索引表(小端格式)
      - 第1个: 固定为0
      - 第2-27个: a-z各字母的结束偏移量(相对于0x6D)
    - 0x6D(109)开始: 实际数据
    
    数据条目格式：
    - 1字节: 字母码长度M
    - 1字节: 汉字字节长度+2(N)
    - M字节: 字母码(ASCII)
    - N-2字节: 汉字(UTF-16-LE)
    - 6字节: 填充(0x00)
    """
    print(f"读取输入文件: {input_txt_file}")
    data_list = read_input_data(input_txt_file)
    print(f"共读取 {len(data_list)} 条记录")
    
    print("计算索引偏移量...")
    offsets, total_size = calculate_offsets(data_list)
    
    print("写入输出文件...")
    write_bin_file(output_bin_file, data_list, offsets)
    
    print(f"\n重建完成！")
    print(f"输出文件: {output_bin_file}")
    print(f"数据区总大小: {total_size} 字节")
    print(f"文件头大小: 109 字节 (0x00 + 27个索引)")
    print("\n各字母结束偏移量(相对于0x6D):")
    for i, letter in enumerate('abcdefghijklmnopqrstuvwxyz'):
        print(f"  {letter}: {offsets[letter]:6d}", end="")
        if (i + 1) % 5 == 0:
            print()  # 每5个换行


if __name__ == "__main__":
    input_txt_file = "def3_output.txt"
    output_bin_file = "rebuilt_def3_with_index_fixed.bin"
    rebuild_bin_file_with_index(input_txt_file, output_bin_file)
