
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface ChatRequest {
  question: string;
  documentId?: string | null;
  limit?: number;
}

export interface ChatSource {
  documentId: string;
  userId?: string;
  pageNumber: number;
  chunkIndex: number;
  score: number;
  text: string;
}

export interface ChatResponseData {
  question: string;
  answer: string;
  sources: ChatSource[];
}

export interface ChatApiResponse {
  success: boolean;
  data: ChatResponseData;
  count?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/chat`;

  sendMessage(request: ChatRequest): Observable<ChatApiResponse> {
    return this.http.post<ChatApiResponse>(
      this.apiUrl,
      {
        question: request.question.trim(),
        documentId: request.documentId || null,
        limit: request.limit ?? 5
      }
    );
  }
}
