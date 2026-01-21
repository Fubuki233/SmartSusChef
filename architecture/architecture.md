# Architecture Overview

it's an overview of the architecture of the SmartSus Chef​ system, which is A Demand Forecasting and Food Prep Recommendation Tool for more sustainable F&B operations​.

## BackGround
Small and medium F&B operators in Singapore often over‑prepare ingredients to avoid stockouts. ​

However, daily demand can be unpredictable due to weather, special events/holidays, and customer behaviour.​

Many operators lack sophisticated Point-of-Sales (POS) systems, and existing tools are either too complex and/or too costly.​

The gap we’re trying to fill: ​

A simple, flexible demand forecasting tool that works with or without POS systems.​

It works by:​

data via:​

Manual entry or CSV import​

POS integration (where available)​

Using time-series forecasting​

Incorporating weather and calendar signals​

Offering predictions on future demand to reduce food wastage ​

## 1. Login Page:
Login page:only for authorized staff:
- Staff members can log in using their credentials.
- Successful login redirects to the dashboard.
- Failed login shows an error message.

## 2. Dashboard:

this sector contains two main parts:

1. mobile user for employee
    time range limit: 7 dasy maximum( today, last 7 days )
   - sell trend dashboard: bar+line chart display sell trend(sum of all recipes) based on time range( today, last 7 days max)
   - input data: allow employee to input only today's sell data/waste data(by dishs) for each recipe. 
   - a pie chart display one day's distribution based on recipe: when click on a single row in sell distribution bar chart, the pie chart will display that day's sell distribution of each recipe(7 days maximum).
   - a table display sell distribution based on ingredient: when click on a single bar in sell trend chart, the table will display sell distribution of that day(7 days maximum)(format: ingredient unit quantity).  
   - calender: display special events/holidays based on singapore public holiday API.
   - weather widget: display current weather based on singapore weather API.
   - prediction detail: display forecasted ingredient quantity(by ingredient: table) for each recipe for next 7 days(editable) based on time series forecasting model(format: ingredient, unit, quantity).
   - wastage trend: will show bar+line chart for wastage trend(by ingredient) based on time range(7 days maximum) and carbon footprint.
   - if user click on bar in wastage chart, a pie chart(by recipe) and a table(by ingredient) will display wastage distribution of that day.



2. web user for manager
    no limit for time range( today, custom range )
   - sell trend dashboard: bar+line chart display sell trend(sum of all recipes) based on time range( today, custom range )
   - input data: allow admin to input any days' sell data/waste data(by dishs) for each recipe. 
   - a pie chart display one day's distribution based on recipe: when click on a single row in sell distribution bar chart, the pie chart will display that day's sell distribution of each recipe.
   - a table display sell distribution based on ingredient: when click on a single bar in sell trend chart, the table will display sell distribution of that day(format: ingredient unit quantity).  
   - calender: display special events/holidays based on singapore public holiday API. 
   - weather widget: display current weather based on singapore weather API.
   - prediction summary:  bar+line chart, display forecasted total sell quantity for next 7 days(editable) based on time series forecasting model.(also show the trend compared to last week/last holiday)
   - prediction detail: display forecasted ingredient quantity(by ingredient: table) for each recipe for next 7 days(editable) based on time series forecasting model(format: ingredient, unit, quantity).
   - wastage trend: will show chart for wastage trend(by ingredient) based on time range and carbon footprint.
   - button for navigation to Management System. 

## 3. Background Management System:

This system will manage the following functionalities:
A side menu for navigation between different management functions.

1. recipe management:
    this function allows admins to manage each dish's recipe:
    ingredients(ingredient, quantity).

2. ingredient management
    this function allows admins to manage ingredients:
    add, edit, delete ingredient information.

3. import sells data
    this function allows users to import sells data from local file or (csv, format will shown on database design).

4. wastage data management
    this function allows users to view and edit wastage data:
    now: only by ingredient basis: select ingredient, system will calculate total ingredient wastage. Web side(admin) can edit history data.

5. export data
    this function allows users to export data:
    export sell data, wastage data, forecast data.
