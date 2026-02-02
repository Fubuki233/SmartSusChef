import React from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle } from '@/app/components/ui/card';
import { Cloud, Droplets, Thermometer, Loader2 } from 'lucide-react';

export function WeatherWidget() {
  const { weather, dataLoading } = useApp();

  if (dataLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Cloud className="w-4 h-4 text-[#4A90E2]" />
            Current Weather
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-4">
            <Loader2 className="w-5 h-5 animate-spin text-gray-400" />
            <span className="ml-2 text-sm text-gray-500">Loading weather...</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!weather || weather.temperature === undefined) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Cloud className="w-4 h-4 text-[#4A90E2]" />
            Current Weather
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-6 text-center">
            <Cloud className="w-12 h-12 text-gray-300 mb-2" />
            <p className="text-sm text-gray-500">Weather data unavailable</p>
            <p className="text-xs text-gray-400 mt-1">Please set store coordinates in Settings</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <Cloud className="w-4 h-4 text-[#4A90E2]" />
          Current Weather
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-4xl font-bold text-[#1A1C18]">{weather.temperature.toFixed(1)}°C</div>
              <div className="text-sm text-gray-600 mt-1">{weather.condition}</div>
            </div>
            <Cloud className="w-16 h-16 text-[#4A90E2]" />
          </div>

          <div className="grid grid-cols-2 gap-4 pt-4 border-t">
            <div className="flex items-center gap-2">
              <Droplets className="w-4 h-4 text-[#4A90E2]" />
              <div>
                <p className="text-xs text-gray-600">Humidity</p>
                <p className="font-semibold text-[#1A1C18]">{weather.humidity}%</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Thermometer className="w-4 h-4 text-[#4A90E2]" />
              <div>
                <p className="text-xs text-gray-600">Feels like</p>
                <p className="font-semibold text-[#1A1C18]">{(weather.temperature + 2).toFixed(1)}°C</p>
              </div>
            </div>
          </div>

          <p className="text-sm text-gray-600 pt-2 border-t">{weather.description}</p>
        </div>
      </CardContent>
    </Card>
  );
}