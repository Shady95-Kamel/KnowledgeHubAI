
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DocumentDto {
  id: string;
  name: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  status: number | string;
  pageCount: number | null;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
}

export interface DocumentsQuery {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  status?: string;
  sortBy?: string;
  descending?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class DocumentsService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/documents`;

  getMyDocuments(
    query: DocumentsQuery = {}
  ): Observable<ApiResponse<PagedResult<DocumentDto>>> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber ?? 1)
      .set('pageSize', query.pageSize ?? 10)
      .set('descending', query.descending ?? true);

    if (query.searchTerm?.trim()) {
      params = params.set(
        'searchTerm',
        query.searchTerm.trim()
      );
    }

    if (query.status) {
      params = params.set('status', query.status);
    }

    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }

    return this.http.get<
      ApiResponse<PagedResult<DocumentDto>>
    >(this.apiUrl, {
      params,
      withCredentials: true
    });
  }

  deleteDocument(id: string): Observable<{
    success: boolean;
    message: string;
  }> {
    return this.http.delete<{
      success: boolean;
      message: string;
    }>(`${this.apiUrl}/${id}`, {
      withCredentials: true
    });
  }

  uploadDocument(file: File): Observable<ApiResponse<DocumentDto>> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<ApiResponse<DocumentDto>>(
      `${this.apiUrl}/upload`,
      formData,
      { withCredentials: true }
    );
  }

  retryDocument(id: string): Observable<{
    success: boolean;
    message: string;
  }> {
    return this.http.post<{
      success: boolean;
      message: string;
    }>(
      `${this.apiUrl}/${id}/retry`,
      {},
      { withCredentials: true }
    );
  }
}
