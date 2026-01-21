import pandas as pd
import matplotlib.pyplot as plt
from datetime import datetime

# 设置中文字体支持
plt.rcParams['font.sans-serif'] = ['SimHei', 'Microsoft YaHei', 'Arial Unicode MS']
plt.rcParams['axes.unicode_minus'] = False

# 读取数据
df = pd.read_csv('restaurant-1-orders.csv')

# 将订单日期转换为日期格式
df['Order Date'] = pd.to_datetime(df['Order Date'], format='%d/%m/%Y %H:%M')

# 提取日期（不含时间）
df['Date'] = df['Order Date'].dt.date

# 按日期汇总每日售出菜品总量
daily_sales = df.groupby('Date')['Quantity'].sum().reset_index()
daily_sales.columns = ['日期', '售出菜品总量']

# 创建图表
plt.figure(figsize=(15, 6))
plt.plot(daily_sales['日期'], daily_sales['售出菜品总量'], marker='o', linewidth=2, markersize=4)

# 设置图表标题和标签
plt.title('每日售出菜品总量趋势图', fontsize=16, fontweight='bold')
plt.xlabel('日期', fontsize=12)
plt.ylabel('售出菜品总量', fontsize=12)

# 旋转x轴标签以避免重叠
plt.xticks(rotation=45, ha='right')

# 添加网格线
plt.grid(True, alpha=0.3, linestyle='--')

# 调整布局
plt.tight_layout()

# 显示图表
plt.show()

# 打印统计信息
print(f"\n数据统计信息:")
print(f"总天数: {len(daily_sales)}")
print(f"日期范围: {daily_sales['日期'].min()} 至 {daily_sales['日期'].max()}")
print(f"平均每日销量: {daily_sales['售出菜品总量'].mean():.2f}")
print(f"最高单日销量: {daily_sales['售出菜品总量'].max()}")
print(f"最低单日销量: {daily_sales['售出菜品总量'].min()}")
