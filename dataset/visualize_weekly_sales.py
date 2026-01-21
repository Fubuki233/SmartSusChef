import pandas as pd
import matplotlib.pyplot as plt
from datetime import datetime, timedelta
import random

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

# 转换日期为datetime类型
daily_sales['日期'] = pd.to_datetime(daily_sales['日期'])

# 获取所有可用的日期范围
min_date = daily_sales['日期'].min()
max_date = daily_sales['日期'].max()

# 随机选择一个起始日期（确保有完整的7天数据）
max_start_date = max_date - timedelta(days=6)
all_possible_starts = daily_sales[daily_sales['日期'] <= max_start_date]['日期'].tolist()

# 随机选择一个起始日期
random_start = random.choice(all_possible_starts)
week_end = random_start + timedelta(days=6)

# 筛选出这一周的数据
week_data = daily_sales[(daily_sales['日期'] >= random_start) & (daily_sales['日期'] <= week_end)]

# 创建图表
plt.figure(figsize=(12, 6))
bars = plt.bar(range(len(week_data)), week_data['售出菜品总量'], 
               color='steelblue', alpha=0.8, edgecolor='black')

# 为每个柱子添加数值标签
for i, bar in enumerate(bars):
    height = bar.get_height()
    plt.text(bar.get_x() + bar.get_width()/2., height,
             f'{int(height)}',
             ha='center', va='bottom', fontsize=10, fontweight='bold')

# 设置x轴标签为日期和星期
weekdays = ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
x_labels = [f"{date.strftime('%m/%d')}\n{weekdays[date.weekday()]}" 
            for date in week_data['日期']]
plt.xticks(range(len(week_data)), x_labels)

# 设置图表标题和标签
plt.title(f'一周销量详情 ({random_start.strftime("%Y-%m-%d")} 至 {week_end.strftime("%Y-%m-%d")})', 
          fontsize=16, fontweight='bold')
plt.xlabel('日期', fontsize=12)
plt.ylabel('售出菜品总量', fontsize=12)

# 添加网格线
plt.grid(True, alpha=0.3, linestyle='--', axis='y')

# 调整布局
plt.tight_layout()

# 显示图表
plt.show()

# 打印统计信息
print(f"\n一周销量统计:")
print(f"日期范围: {random_start.strftime('%Y-%m-%d')} 至 {week_end.strftime('%Y-%m-%d')}")
print(f"周总销量: {week_data['售出菜品总量'].sum()}")
print(f"平均每日销量: {week_data['售出菜品总量'].mean():.2f}")
print(f"最高单日销量: {week_data['售出菜品总量'].max()} ({week_data.loc[week_data['售出菜品总量'].idxmax(), '日期'].strftime('%Y-%m-%d')})")
print(f"最低单日销量: {week_data['售出菜品总量'].min()} ({week_data.loc[week_data['售出菜品总量'].idxmin(), '日期'].strftime('%Y-%m-%d')})")
print("\n每日详细数据:")
for _, row in week_data.iterrows():
    print(f"  {row['日期'].strftime('%Y-%m-%d')} ({weekdays[row['日期'].weekday()]}): {row['售出菜品总量']} 件")
