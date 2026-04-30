# 集成测试故障排查指南

本文档收录集成测试中常见问题及解决方案。

---

## 1. CLI 工具未找到

**症状**：
```
错误: 找不到 dotnet 命令或 CLI 输出文件
```

**原因**：ImeWlConverterCmd 尚未构建。

**解决方案**：
```bash
# 在仓库根目录执行
make build-cmd
```

---

## 2. 测试用例找不到预期输出文件

**症状**：
```
错误: 预期输出文件不存在: test-cases/1-imports/expected/t01-scel-to-csv.expected
```

**原因**：`expected/` 目录中缺少预期输出文件。

**解决方案**：重新生成预期输出：
```bash
cd tests/integration
# 运行一次测试并保留输出
./run-tests.sh -s 1-imports --keep-output
# 将实际输出复制为预期输出
cp test-output/<filename>.txt test-cases/1-imports/expected/t01-scel-to-csv.expected
```

---

## 3. 测试输出与预期不一致

**症状**：
```
✗ T01-搜狗scel到CSV
  差异：...
```

**原因**：转换逻辑改变或预期文件过期。

**解决方案**：
```bash
# 使用 -v 查看详细差异
./run-tests.sh -s 1-imports -v --keep-output

# 查看差异
diff test-output/<filename>.txt test-cases/1-imports/expected/t01-scel-to-csv.expected

# 如果新输出是正确的，更新预期文件
cp test-output/<filename>.txt test-cases/1-imports/expected/t01-scel-to-csv.expected
```

---

## 4. 编码问题

**症状**：输出文件包含乱码或字符不匹配。

**原因**：输入文件编码（GBK/UTF-8/UTF-16）识别错误。

**解决方案**：
```bash
# 检查文件编码
file -i <input-file>

# 手动指定编码（如需要）
dotnet src/ImeWlConverterCmd/bin/Debug/net10.0/ImeWlConverterCmd.dll \
  -i word -o self -O output.txt input.txt
```

---

## 5. 超时错误

**症状**：
```
错误: 测试超时（超过 30 秒）
```

**原因**：大文件转换时间超过默认超时限制。

**解决方案**：在 `test-config.yaml` 中增加 `timeout` 值：
```yaml
test_cases:
  - name: 大文件测试
    timeout: 120
    ...
```

---

## 6. 权限错误

**症状**：
```
bash: ./run-tests.sh: Permission denied
```

**解决方案**：
```bash
chmod +x tests/integration/run-tests.sh
chmod +x tests/integration/lib/*.sh
```

---

## 7. 路径错误（Windows Git Bash）

**症状**：路径中出现 `/c/Users/...` 格式错误。

**解决方案**：确保在仓库根目录运行脚本，使用相对路径。在 Git Bash 中：
```bash
cd /path/to/imewlconverter
bash tests/integration/run-tests.sh --all
```

---

## 8. 查看详细调试信息

```bash
# 启用详细输出
./run-tests.sh --all -v

# 保留临时输出文件
./run-tests.sh --all --keep-output

# 运行单个套件
./run-tests.sh -s 1-imports -v
```

临时输出文件保存在 `tests/integration/test-output/` 目录（已被 `.gitignore` 忽略）。

---

## 9. 重置测试环境

```bash
# 清理所有临时输出文件
rm -rf tests/integration/test-output/
mkdir -p tests/integration/test-output/

# 重建并重新运行
make build-cmd
cd tests/integration && ./run-tests.sh --all
```
