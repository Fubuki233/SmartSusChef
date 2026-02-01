import React, { useState } from 'react';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/app/components/ui/card';
import { ChefHat, AlertCircle, MapPin, Building2, Store, Globe } from 'lucide-react';
import { useApp } from '@/app/context/AppContext';
import { toast } from 'sonner';

export function StoreSetupPage() {
  const { setupStore, user } = useApp();
  const [formData, setFormData] = useState({
    storeName: '',
    companyName: '',
    uen: '',
    outletLocation: '',
    address: '',
    contactNumber: '',
    latitude: '',
    longitude: '',
  });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    // Validation
    if (!formData.storeName.trim()) {
      setError('Store name is required');
      return;
    }

    const lat = parseFloat(formData.latitude);
    const lng = parseFloat(formData.longitude);

    if (formData.latitude && (isNaN(lat) || lat < -90 || lat > 90)) {
      setError('Latitude must be between -90 and 90');
      return;
    }

    if (formData.longitude && (isNaN(lng) || lng < -180 || lng > 180)) {
      setError('Longitude must be between -180 and 180');
      return;
    }

    setIsLoading(true);
    try {
      await setupStore({
        storeName: formData.storeName,
        companyName: formData.companyName,
        uen: formData.uen,
        outletLocation: formData.outletLocation,
        address: formData.address,
        contactNumber: formData.contactNumber,
        latitude: formData.latitude ? lat : undefined,
        longitude: formData.longitude ? lng : undefined,
      });
      toast.success('Store setup complete! Country code will be automatically detected from coordinates.');
    } catch (err) {
      setError('Failed to setup store. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#F9FBF7] p-4">
      <Card className="w-full max-w-2xl shadow-lg rounded-[8px]">
        <CardHeader className="space-y-4 text-center">
          <div className="flex justify-center">
            <div className="bg-[#4F6F52] p-4 rounded-full shadow-md">
              <ChefHat className="w-12 h-12 text-white" />
            </div>
          </div>
          <CardTitle className="text-3xl text-[#1A1C18]">Welcome, {user?.name}!</CardTitle>
          <CardDescription className="text-[#6b7280]">
            Let's set up your store to get started. The country code will be automatically detected from your coordinates.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Store Info Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2 text-[#4F6F52] font-medium">
                  <Store className="w-5 h-5" />
                  <span>Store Information</span>
                </div>
                
                <div className="space-y-2">
                  <Label htmlFor="storeName">Store Name *</Label>
                  <Input
                    id="storeName"
                    type="text"
                    placeholder="e.g., SmartSus Kitchen"
                    value={formData.storeName}
                    onChange={(e) => setFormData({...formData, storeName: e.target.value})}
                    required
                    className="rounded-[8px]"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="companyName">Company Name</Label>
                  <Input
                    id="companyName"
                    type="text"
                    placeholder="e.g., SmartSus Pte Ltd"
                    value={formData.companyName}
                    onChange={(e) => setFormData({...formData, companyName: e.target.value})}
                    className="rounded-[8px]"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="uen">UEN (Unique Entity Number)</Label>
                  <Input
                    id="uen"
                    type="text"
                    placeholder="e.g., 202012345A"
                    value={formData.uen}
                    onChange={(e) => setFormData({...formData, uen: e.target.value})}
                    className="rounded-[8px]"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="contactNumber">Contact Number</Label>
                  <Input
                    id="contactNumber"
                    type="text"
                    placeholder="e.g., +65 6123 4567"
                    value={formData.contactNumber}
                    onChange={(e) => setFormData({...formData, contactNumber: e.target.value})}
                    className="rounded-[8px]"
                  />
                </div>
              </div>

              {/* Location Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2 text-[#4F6F52] font-medium">
                  <MapPin className="w-5 h-5" />
                  <span>Location Details</span>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="outletLocation">Outlet Location</Label>
                  <Input
                    id="outletLocation"
                    type="text"
                    placeholder="e.g., Orchard Road Branch"
                    value={formData.outletLocation}
                    onChange={(e) => setFormData({...formData, outletLocation: e.target.value})}
                    className="rounded-[8px]"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="address">Full Address</Label>
                  <Input
                    id="address"
                    type="text"
                    placeholder="e.g., 123 Orchard Road, #01-01"
                    value={formData.address}
                    onChange={(e) => setFormData({...formData, address: e.target.value})}
                    className="rounded-[8px]"
                  />
                </div>

                <div className="p-3 bg-blue-50 border border-blue-200 rounded-[8px] text-sm text-blue-700">
                  <div className="flex items-center gap-2 mb-2">
                    <Globe className="w-4 h-4" />
                    <span className="font-medium">Coordinates for Weather & Holidays</span>
                  </div>
                  <p className="text-xs">
                    Enter your store's coordinates to enable automatic weather forecasting and local holiday detection.
                    Country code will be automatically determined from coordinates.
                  </p>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="latitude">Latitude</Label>
                    <Input
                      id="latitude"
                      type="text"
                      placeholder="e.g., 1.3521"
                      value={formData.latitude}
                      onChange={(e) => setFormData({...formData, latitude: e.target.value})}
                      className="rounded-[8px]"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="longitude">Longitude</Label>
                    <Input
                      id="longitude"
                      type="text"
                      placeholder="e.g., 103.8198"
                      value={formData.longitude}
                      onChange={(e) => setFormData({...formData, longitude: e.target.value})}
                      className="rounded-[8px]"
                    />
                  </div>
                </div>

                <p className="text-xs text-gray-500">
                  Tip: You can find coordinates using Google Maps by right-clicking on your location.
                </p>
              </div>
            </div>

            {error && (
              <div className="flex items-center gap-2 text-red-500 text-sm bg-red-50 p-3 rounded-[8px]">
                <AlertCircle className="w-4 h-4" />
                <span>{error}</span>
              </div>
            )}

            <Button 
              type="submit" 
              className="w-full bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] h-12 gap-2 text-base"
              disabled={isLoading}
            >
              {isLoading ? 'Setting up store...' : (
                <>
                  <Building2 className="w-5 h-5" />
                  Complete Store Setup
                </>
              )}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
