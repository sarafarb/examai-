export interface ExportSettings {
  penColor: 'Red' | 'Blue' | 'Black';
  handwritingStyle: 'Classic' | 'Quick Scribble' | 'Elegant';
}

export interface ExportJobResponse {
  exportJobId: string;
}

export interface ExportJobStatus {
  exportJobId: string;
  status: 'PENDING' | 'PROCESSING' | 'COMPLETED' | 'FAILED';
  downloadUrl?: string;
  progressPercentage?: number;
  errorMessage?: string;
}