import React, { useState, useEffect, useCallback, useRef } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { mlApi, MlStatusDto, MlModelStatus } from '@/app/services/api';
import { useApp } from '@/app/context/AppContext';
import {
    Brain,
    CheckCircle2,
    AlertTriangle,
    Loader2,
    RefreshCw,
    Database,
    Zap,
    XCircle,
} from 'lucide-react';

const STATUS_CONFIG: Record<MlModelStatus, {
    icon: React.ElementType;
    color: string;
    bgColor: string;
    borderColor: string;
    label: string;
}> = {
    ready: {
        icon: CheckCircle2,
        color: 'text-green-600',
        bgColor: 'bg-green-50',
        borderColor: 'border-green-200',
        label: 'Models Ready',
    },
    training: {
        icon: Loader2,
        color: 'text-blue-600',
        bgColor: 'bg-blue-50',
        borderColor: 'border-blue-200',
        label: 'Training in Progress',
    },
    can_train: {
        icon: Zap,
        color: 'text-amber-600',
        bgColor: 'bg-amber-50',
        borderColor: 'border-amber-200',
        label: 'Ready to Train',
    },
    insufficient_data: {
        icon: Database,
        color: 'text-orange-600',
        bgColor: 'bg-orange-50',
        borderColor: 'border-orange-200',
        label: 'Insufficient Data',
    },
    unavailable: {
        icon: XCircle,
        color: 'text-gray-500',
        bgColor: 'bg-gray-50',
        borderColor: 'border-gray-200',
        label: 'ML Service Unavailable',
    },
};

export function MlModelStatusCard() {
    const { refreshData } = useApp();
    const [status, setStatus] = useState<MlStatusDto | null>(null);
    const [loading, setLoading] = useState(true);
    const [training, setTraining] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const fetchStatus = useCallback(async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await mlApi.getStatus();
            setStatus(data);
        } catch (err) {
            console.error('[MlModelStatus] Failed to fetch status:', err);
            setError('Unable to connect to ML service');
            setStatus(null);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchStatus();
    }, [fetchStatus]);

    // Track previous status to detect training → ready transition
    const prevStatusRef = useRef<string | null>(null);

    // Auto-refresh while training
    useEffect(() => {
        if (status?.status === 'training' || training) {
            const interval = setInterval(fetchStatus, 10000); // Poll every 10s
            return () => clearInterval(interval);
        }
    }, [status?.status, training, fetchStatus]);

    // Auto-generate predictions when training completes
    useEffect(() => {
        const prev = prevStatusRef.current;
        const curr = status?.status ?? null;
        prevStatusRef.current = curr;

        if (prev === 'training' && curr === 'ready') {
            // Training just finished — automatically fetch predictions
            (async () => {
                try {
                    setLoading(true);
                    await mlApi.predict(7);
                    await refreshData();
                    await fetchStatus();
                } catch (err) {
                    console.error('[MlModelStatus] Auto-predict after training failed:', err);
                } finally {
                    setLoading(false);
                }
            })();
        }
    }, [status?.status, refreshData, fetchStatus]);

    const handleTrain = async () => {
        try {
            setTraining(true);
            setError(null);
            await mlApi.train();
            // Refresh status after triggering training
            await fetchStatus();
        } catch (err) {
            console.error('[MlModelStatus] Failed to trigger training:', err);
            setError('Failed to start training. Please try again.');
        } finally {
            setTraining(false);
        }
    };

    const handleRefreshPredictions = async () => {
        try {
            setLoading(true);
            setError(null);
            await mlApi.predict(7);
            // Refresh all app data so charts get the new predictions
            await refreshData();
            await fetchStatus();
        } catch (err) {
            console.error('[MlModelStatus] Failed to refresh predictions:', err);
            setError('Failed to refresh predictions.');
        } finally {
            setLoading(false);
        }
    };

    if (loading && !status) {
        return (
            <Card className="rounded-[8px]">
                <CardContent className="flex items-center justify-center py-8">
                    <Loader2 className="w-5 h-5 animate-spin text-gray-400 mr-2" />
                    <span className="text-sm text-gray-500">Checking ML model status...</span>
                </CardContent>
            </Card>
        );
    }

    const mlStatus: MlModelStatus = status?.status ?? 'unavailable';
    const config = STATUS_CONFIG[mlStatus];
    const StatusIcon = config.icon;

    return (
        <Card className={`rounded-[8px] border ${config.borderColor}`}>
            <CardHeader className="pb-3">
                <div className="flex items-start justify-between">
                    <div>
                        <CardTitle className="flex items-center gap-2 text-base">
                            <Brain className="w-5 h-5 text-[#4F6F52]" />
                            AI Prediction Engine
                        </CardTitle>
                        <CardDescription className="mt-1">
                            Machine learning model status for your store
                        </CardDescription>
                    </div>
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={fetchStatus}
                        disabled={loading}
                        className="h-8 w-8 p-0"
                        title="Refresh status"
                    >
                        <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
                    </Button>
                </div>
            </CardHeader>
            <CardContent className="space-y-4">
                {/* Status Badge */}
                <div className={`flex items-center gap-3 p-3 rounded-lg ${config.bgColor}`}>
                    <StatusIcon className={`w-5 h-5 ${config.color} ${mlStatus === 'training' ? 'animate-spin' : ''}`} />
                    <div className="flex-1 min-w-0">
                        <p className={`text-sm font-semibold ${config.color}`}>{config.label}</p>
                        <p className="text-xs text-gray-600 mt-0.5">
                            {status?.message ?? error ?? 'ML service is not connected.'}
                        </p>
                    </div>
                </div>

                {/* Data Info */}
                {status?.daysAvailable != null && (
                    <div className="flex items-center justify-between text-sm">
                        <span className="text-gray-500">Sales data available</span>
                        <span className="font-medium">
                            {status.daysAvailable} days
                            {status.daysAvailable < 100 && (
                                <span className="text-orange-500 ml-1">(min. 100 needed)</span>
                            )}
                        </span>
                    </div>
                )}

                {/* Dish count */}
                {status?.dishes && status.dishes.length > 0 && (
                    <div className="flex items-center justify-between text-sm">
                        <span className="text-gray-500">Trained dishes</span>
                        <span className="font-medium">{status.dishes.length} dishes</span>
                    </div>
                )}

                {/* Progress bar for data sufficiency */}
                {status?.daysAvailable != null && !status.hasModels && (
                    <div className="space-y-1">
                        <div className="flex justify-between text-xs text-gray-500">
                            <span>Data collection progress</span>
                            <span>{Math.min(100, Math.round((status.daysAvailable / 100) * 100))}%</span>
                        </div>
                        <div className="h-2 bg-gray-200 rounded-full overflow-hidden">
                            <div
                                className={`h-full rounded-full transition-all duration-500 ${status.daysAvailable >= 100 ? 'bg-green-500' : 'bg-amber-500'
                                    }`}
                                style={{ width: `${Math.min(100, (status.daysAvailable / 100) * 100)}%` }}
                            />
                        </div>
                    </div>
                )}

                {/* Actions */}
                <div className="flex gap-2 pt-1">
                    {/* Show train button when data is sufficient but no models */}
                    {mlStatus === 'can_train' && (
                        <Button
                            onClick={handleTrain}
                            disabled={training}
                            className="bg-[#4F6F52] hover:bg-[#3D563F] text-white flex-1"
                            size="sm"
                        >
                            {training ? (
                                <>
                                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                                    Starting Training...
                                </>
                            ) : (
                                <>
                                    <Zap className="w-4 h-4 mr-2" />
                                    Train Models
                                </>
                            )}
                        </Button>
                    )}

                    {/* Show refresh predictions button when models are ready */}
                    {mlStatus === 'ready' && (
                        <Button
                            onClick={handleRefreshPredictions}
                            disabled={loading}
                            variant="outline"
                            className="flex-1 border-[#4F6F52] text-[#4F6F52] hover:bg-[#4F6F52]/5"
                            size="sm"
                        >
                            {loading ? (
                                <>
                                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                                    Refreshing...
                                </>
                            ) : (
                                <>
                                    <RefreshCw className="w-4 h-4 mr-2" />
                                    Refresh AI Predictions
                                </>
                            )}
                        </Button>
                    )}

                    {/* Show training status with progress when training */}
                    {mlStatus === 'training' && (
                        <div className="flex-1 space-y-2 py-1">
                            <p className="text-sm text-blue-600 flex items-center justify-center gap-2">
                                <Loader2 className="w-4 h-4 animate-spin" />
                                Training in progress...
                            </p>
                            {status?.trainingProgress && status.trainingProgress.total > 0 && (
                                <div className="space-y-1.5">
                                    <div className="flex justify-between text-xs text-gray-600">
                                        <span>
                                            Dish {status.trainingProgress.trained + status.trainingProgress.failed}/{status.trainingProgress.total}
                                            {status.trainingProgress.failed > 0 && (
                                                <span className="text-orange-500 ml-1">
                                                    ({status.trainingProgress.failed} failed)
                                                </span>
                                            )}
                                        </span>
                                        <span>{Math.round(((status.trainingProgress.trained + status.trainingProgress.failed) / status.trainingProgress.total) * 100)}%</span>
                                    </div>
                                    <div className="h-2 bg-gray-200 rounded-full overflow-hidden">
                                        <div
                                            className="h-full rounded-full bg-blue-500 transition-all duration-700"
                                            style={{ width: `${((status.trainingProgress.trained + status.trainingProgress.failed) / status.trainingProgress.total) * 100}%` }}
                                        />
                                    </div>
                                    {status.trainingProgress.currentDish && (
                                        <p className="text-xs text-gray-500 truncate">
                                            Training: {status.trainingProgress.currentDish}
                                        </p>
                                    )}
                                </div>
                            )}
                        </div>
                    )}
                </div>

                {/* Error display */}
                {error && (
                    <div className="flex items-center gap-2 text-sm text-red-600 bg-red-50 p-2 rounded">
                        <AlertTriangle className="w-4 h-4 flex-shrink-0" />
                        <span>{error}</span>
                    </div>
                )}
            </CardContent>
        </Card>
    );
}
