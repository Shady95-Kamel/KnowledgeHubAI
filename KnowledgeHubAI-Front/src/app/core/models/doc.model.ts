export type DocumentStatus = 'Completed' | 'Processing' | 'Failed';

export interface DocumentItem {
  id: string;
  name: string;
  uploadedAt: string;
  size: string;
  status: DocumentStatus;
}

