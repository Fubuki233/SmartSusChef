import React from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle } from '@/app/components/ui/card';
import { Cloud, Droplets, Thermometer } from 'lucide-react';

export function WeatherWidget() {
  const { weather } = useApp();

  if (!weather) {
    return null;
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <Cloud className="w-4 h-4" />
          Current Weather
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-4xl font-bold">{weather.temperature}°C</div>
              <div className="text-sm text-gray-600 mt-1">{weather.condition}</div>
            </div>
            <div className="text-right">
              <Cloud className="w-16 h-16 text-blue-400" />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3 pt-3 border-t">
            <div className="flex items-center gap-2">
              <Droplets className="w-4 h-4 text-blue-600" />
              <div>
                <div className="text-xs text-gray-600">Humidity</div>
                <div className="font-semibold">{weather.humidity}%</div>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Thermometer className="w-4 h-4 text-red-600" />
              <div>
                <div className="text-xs text-gray-600">Feels like</div>
                <div className="font-semibold">{weather.temperature + 2}°C</div>
              </div>
            </div>
          </div>

          <p className="text-xs text-gray-600 pt-2 border-t">{weather.description}</p>
        </div>
      </CardContent>
    </Card>
  );
}
