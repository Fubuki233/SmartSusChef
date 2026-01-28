import React, { useState } from 'react';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/app/components/ui/card';
import { Store, MapPin, Phone, Calendar, Building2, AlertCircle, Loader2, CheckCircle } from 'lucide-react';
import { api, UpdateStoreRequest } from '@/app/services/api';
import { toast } from 'sonner';

interface StoreSetupPageProps {
    onSetupComplete: () => void;
}

export function StoreSetupPage({ onSetupComplete }: StoreSetupPageProps) {
    const [step, setStep] = useState(1);
    const [formData, setFormData] = useState<UpdateStoreRequest>({
        companyName: '',
        uen: '',
        storeName: '',
        outletLocation: '',
        contactNumber: '',
        address: '',
        latitude: 1.3521,
        longitude: 103.8198,
        openingDate: new Date().toISOString().split('T')[0],
    });
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
        setError('');
    };

    const validateStep1 = (): boolean => {
        if (!formData.storeName?.trim()) {
            setError('Store name is required');
            return false;
        }
        if (!formData.companyName?.trim()) {
            setError('Company name is required');
            return false;
        }
        return true;
    };

    const validateStep2 = (): boolean => {
        if (!formData.outletLocation?.trim()) {
            setError('Outlet location is required');
            return false;
        }
        if (!formData.contactNumber?.trim()) {
            setError('Contact number is required');
            return false;
        }
        return true;
    };

    const handleNext = () => {
        if (step === 1 && validateStep1()) {
            setStep(2);
        }
    };

    const handleBack = () => {
        setStep(1);
        setError('');
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');

        if (!validateStep2()) {
            return;
        }

        setIsLoading(true);
        try {
            await api.store.setup({
                ...formData,
                openingDate: formData.openingDate || new Date().toISOString(),
            });

            toast.success('Store setup complete! Welcome to SmartSus Chef.');
            onSetupComplete();
        } catch (err: any) {
            const message = err.message || 'Failed to set up store. Please try again.';
            setError(message);
            toast.error(message);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-[#F9FBF7] p-4">
            <Card className="w-full max-w-lg shadow-lg rounded-[8px]">
                <CardHeader className="space-y-4 text-center">
                    <div className="flex justify-center">
                        <div className="bg-[#4F6F52] p-4 rounded-full shadow-md">
                            <Store className="w-12 h-12 text-white" />
                        </div>
                    </div>
                    <CardTitle className="text-2xl text-[#1A1C18]">Set Up Your Store</CardTitle>
                    <CardDescription className="text-[#6b7280]">
                        Please provide your store information to continue
                    </CardDescription>

                    {/* Progress indicator */}
                    <div className="flex items-center justify-center gap-2 pt-2">
                        <div className={`w-3 h-3 rounded-full ${step >= 1 ? 'bg-[#4F6F52]' : 'bg-gray-300'}`} />
                        <div className={`w-12 h-1 ${step >= 2 ? 'bg-[#4F6F52]' : 'bg-gray-300'}`} />
                        <div className={`w-3 h-3 rounded-full ${step >= 2 ? 'bg-[#4F6F52]' : 'bg-gray-300'}`} />
                    </div>
                    <p className="text-sm text-gray-500">Step {step} of 2</p>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-4">
                        {error && (
                            <div className="flex items-center gap-2 p-3 bg-red-50 text-red-700 rounded-lg text-sm">
                                <AlertCircle className="w-4 h-4 flex-shrink-0" />
                                <span>{error}</span>
                            </div>
                        )}

                        {step === 1 ? (
                            /* Step 1: Basic Info */
                            <>
                                <div className="space-y-2">
                                    <Label htmlFor="storeName" className="flex items-center gap-2">
                                        <Store className="w-4 h-4" />
                                        Store Name *
                                    </Label>
                                    <Input
                                        id="storeName"
                                        name="storeName"
                                        type="text"
                                        placeholder="e.g., Aunty May's Cafe"
                                        value={formData.storeName || ''}
                                        onChange={handleChange}
                                        required
                                        className="rounded-[8px]"
                                    />
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="companyName" className="flex items-center gap-2">
                                        <Building2 className="w-4 h-4" />
                                        Company Name *
                                    </Label>
                                    <Input
                                        id="companyName"
                                        name="companyName"
                                        type="text"
                                        placeholder="e.g., Aunty May's Pte Ltd"
                                        value={formData.companyName || ''}
                                        onChange={handleChange}
                                        required
                                        className="rounded-[8px]"
                                    />
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="uen">UEN (Business Registration Number)</Label>
                                    <Input
                                        id="uen"
                                        name="uen"
                                        type="text"
                                        placeholder="e.g., 202012345Z"
                                        value={formData.uen || ''}
                                        onChange={handleChange}
                                        className="rounded-[8px]"
                                    />
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="openingDate" className="flex items-center gap-2">
                                        <Calendar className="w-4 h-4" />
                                        Store Opening Date
                                    </Label>
                                    <Input
                                        id="openingDate"
                                        name="openingDate"
                                        type="date"
                                        value={formData.openingDate?.split('T')[0] || ''}
                                        onChange={handleChange}
                                        className="rounded-[8px]"
                                    />
                                </div>

                                <Button
                                    type="button"
                                    onClick={handleNext}
                                    className="w-full bg-[#4F6F52] hover:bg-[#3d5a40] text-white rounded-[32px] h-11 mt-4"
                                >
                                    Continue
                                </Button>
                            </>
                        ) : (
                            /* Step 2: Location & Contact */
                            <>
                                <div className="space-y-2">
                                    <Label htmlFor="outletLocation" className="flex items-center gap-2">
                                        <MapPin className="w-4 h-4" />
                                        Outlet Location *
                                    </Label>
                                    <Input
                                        id="outletLocation"
                                        name="outletLocation"
                                        type="text"
                                        placeholder="e.g., Orchard Central"
                                        value={formData.outletLocation || ''}
                                        onChange={handleChange}
                                        required
                                        className="rounded-[8px]"
                                        disabled={isLoading}
                                    />
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="address">Full Address</Label>
                                    <Input
                                        id="address"
                                        name="address"
                                        type="text"
                                        placeholder="e.g., 181 Orchard Rd, #04-01, Singapore 238896"
                                        value={formData.address || ''}
                                        onChange={handleChange}
                                        className="rounded-[8px]"
                                        disabled={isLoading}
                                    />
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="contactNumber" className="flex items-center gap-2">
                                        <Phone className="w-4 h-4" />
                                        Contact Number *
                                    </Label>
                                    <Input
                                        id="contactNumber"
                                        name="contactNumber"
                                        type="tel"
                                        placeholder="e.g., +65 6733 1234"
                                        value={formData.contactNumber || ''}
                                        onChange={handleChange}
                                        required
                                        className="rounded-[8px]"
                                        disabled={isLoading}
                                    />
                                </div>

                                <div className="grid grid-cols-2 gap-4">
                                    <div className="space-y-2">
                                        <Label htmlFor="latitude">Latitude</Label>
                                        <Input
                                            id="latitude"
                                            name="latitude"
                                            type="number"
                                            step="any"
                                            placeholder="1.3521"
                                            value={formData.latitude || ''}
                                            onChange={handleChange}
                                            className="rounded-[8px]"
                                            disabled={isLoading}
                                        />
                                    </div>
                                    <div className="space-y-2">
                                        <Label htmlFor="longitude">Longitude</Label>
                                        <Input
                                            id="longitude"
                                            name="longitude"
                                            type="number"
                                            step="any"
                                            placeholder="103.8198"
                                            value={formData.longitude || ''}
                                            onChange={handleChange}
                                            className="rounded-[8px]"
                                            disabled={isLoading}
                                        />
                                    </div>
                                </div>

                                <div className="bg-green-50 p-3 rounded-lg text-sm text-green-700">
                                    <div className="flex items-start gap-2">
                                        <CheckCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
                                        <span>
                                            After setup, you can manage employees, track sales,
                                            monitor wastage, and view demand forecasts.
                                        </span>
                                    </div>
                                </div>

                                <div className="flex gap-3 mt-4">
                                    <Button
                                        type="button"
                                        variant="outline"
                                        onClick={handleBack}
                                        className="flex-1 rounded-[32px] h-11"
                                        disabled={isLoading}
                                    >
                                        Back
                                    </Button>
                                    <Button
                                        type="submit"
                                        className="flex-1 bg-[#4F6F52] hover:bg-[#3d5a40] text-white rounded-[32px] h-11"
                                        disabled={isLoading}
                                    >
                                        {isLoading ? (
                                            <>
                                                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                                                Setting Up...
                                            </>
                                        ) : (
                                            'Complete Setup'
                                        )}
                                    </Button>
                                </div>
                            </>
                        )}
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
