# 码表处理工具使用说明

本项目包含两个Python脚本，用于处理超强音形码表文件(`def3.bin`)，允许用户自定义修改码表并重新构建可在百度手机输入法中使用的bin文件。

## 工具介绍

### 1. parse_def3_new.py - 解析码表文件
此脚本用于解析原始的`def3.bin`码表文件，并将其转换为易于编辑的文本格式。

#### 功能说明
- 从`def3.bin`文件中提取码表数据
- 将二进制数据解析为可读的文本格式
- 输出格式为：`字母码 汉字`（每行一条记录）

#### 使用方法
```bash
python parse_def3_new.py [input_file] [output_file]
```

参数说明：
- `input_file`（可选）：输入的bin文件路径，默认为`def3.bin`
- `output_file`（可选）：输出的文本文件路径，默认为`def3_output.txt`

#### 示例
```bash
# 使用默认文件名
python parse_def3_new.py

# 指定输入和输出文件
python parse_def3_new.py my_def3.bin parsed_output.txt
```

### 2. rebuild_bin_with_index_fixed.py - 重建码表文件
此脚本用于将修改后的文本码表重新构建成bin文件，供百度手机输入法使用。

#### 功能说明
- 读取文本格式的码表文件
- 根据首字母建立索引表
- 重新构建符合百度手机输入法要求的bin文件格式
- 在文件头部维护正确的索引表

#### 使用方法
```bash
python rebuild_bin_with_index_fixed.py
```

默认情况下：
- 输入文件：`def3_output.txt`（由`parse_def3_new.py`生成的文件）
- 输出文件：`rebuilt_def3_with_index_fixed.bin`

#### 自定义文件名
如需使用不同的输入或输出文件，需要修改脚本中的以下变量：
```python
input_txt_file = "your_input_file.txt"      # 修改为你的输入文件名
output_bin_file = "your_output_file.bin"    # 修改为你的输出文件名
```

## 使用流程

1. **解析原始码表**
   ```bash
   python parse_def3_new.py
   ```
   运行后将生成`def3_output.txt`文件。

2. **编辑码表**
   用户可根据需要编辑`def3_output.txt`文件：
   - 修改现有的字母码
   - 添加新的码表项
   - 删除不需要的码表项
   
   注意事项：
   - 每行格式为：`字母码 汉字`
   - 字母码只能包含小写字母(a-z)
   - 可以添加注释行（以#开头），脚本会自动忽略

3. **重建bin文件**
   ```bash
   python rebuild_bin_with_index_fixed.py
   ```
   运行后将生成`rebuilt_def3_with_index_fixed.bin`文件。

4. **使用自定义码表**
   将生成的`rebuilt_def3_with_index_fixed.bin`文件重命名为`def3.bin`，然后导入到百度手机输入法中使用。

## 注意事项

- 原始`def3.bin`文件是超强音形码表，适用于百度手机输入法
- 编辑码表时请保持格式正确，避免出现错误
- 重建的bin文件经过索引优化，可提高输入法的查找效率
- 建议在修改码表前备份原始文件