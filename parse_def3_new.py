#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
从 def3.bin 文件中解析数据的脚本
按照指定格式解析数据并输出到文本文件

数据结构：
- 第一个字节：码的长度M
- 第二个字节：汉字的字节长度+2（N）
- M个字节：字母（从a到z）
- N-2个字节：汉字（UTF-16LE编码）
- 6个字节的0x00作为结束标记
"""

import struct
import sys


def parse_def3(file_path, output_path):
    """
    解析 def3.bin 文件并将结果保存到文本文件中
    
    Args:
        file_path (str): 输入的 def3.bin 文件路径
        output_path (str): 输出的文本文件路径
    """
    with open(file_path, 'rb') as f:
        # 跳过前0x6D个字节
        f.seek(0x6D)
        
        with open(output_path, 'w', encoding='utf-8') as out_f:
            record_count = 0
            while True:
                # 记录当前位置用于调试
                pos = f.tell()
                
                # 读取 M 字母长度 (1字节)
                m_len_bytes = f.read(1)
                if not m_len_bytes:
                    break
                    
                m_len = struct.unpack('B', m_len_bytes)[0]
                
                # 读取 N 汉字字节长度+2 (1字节)
                n_len_bytes = f.read(1)
                if not n_len_bytes:
                    break
                    
                n_len = struct.unpack('B', n_len_bytes)[0]
                
                # 读取 M 个字母
                letters_bytes = f.read(m_len)
                if not letters_bytes or len(letters_bytes) != m_len:
                    break
                    
                # 解码字母（假设为ASCII）
                letters = letters_bytes.decode('ascii')
                
                # 读取 N-2 字节的 UTF-16LE 汉字
                hanzi_byte_count = n_len - 2
                hanzi_bytes = f.read(hanzi_byte_count)
                if not hanzi_bytes or len(hanzi_bytes) != hanzi_byte_count:
                    break
                
                # 解码UTF-16LE汉字
                if hanzi_byte_count % 2 != 0:
                    print(f"警告: 汉字字节数为奇数，位置: {pos}")
                    # 跳过6个字节的0x00
                    f.read(6)
                    continue
                    
                try:
                    hanzi = hanzi_bytes.decode('utf-16le')
                except UnicodeDecodeError as e:
                    print(f"警告: 无法解码汉字数据，位置: {pos}, 错误: {e}")
                    # 跳过6个字节的0x00
                    f.read(6)
                    continue
                
                # 写入输出文件：字母 汉字
                out_f.write(f"{letters} {hanzi}\n")
                record_count += 1
                
                # 跳过6个字节的0x00
                zero_bytes = f.read(6)
                if not zero_bytes or len(zero_bytes) != 6:
                    break
            
            print(f"总共处理了 {record_count} 条记录")


if __name__ == "__main__":
    input_file = "def3.bin"
    output_file = "def3_output.txt"
    
    if len(sys.argv) == 3:
        input_file = sys.argv[1]
        output_file = sys.argv[2]
    elif len(sys.argv) != 1:
        print("Usage: python parse_def3_new.py [input_file] [output_file]")
        sys.exit(1)
    
    parse_def3(input_file, output_file)
    print(f"解析结果已保存到 {output_file}")