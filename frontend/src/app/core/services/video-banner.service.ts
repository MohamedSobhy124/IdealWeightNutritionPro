import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface VideoBanner {
  hasVideo: boolean;
  hasPoster: boolean;
  videoUrl?: string;
  posterUrl?: string;
  videoSizeBytes?: number;
  posterSizeBytes?: number;
  videoLastModified?: string;
  posterLastModified?: string;
}

@Injectable({ providedIn: 'root' })
export class VideoBannerService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly httpOptions = { withCredentials: true };

  getPublicBanner() {
    return this.http.get<VideoBanner>(`${this.baseUrl}/content/video-banner`);
  }

  getAdminBanner() {
    return this.http.get<VideoBanner>(`${this.baseUrl}/admin/video-banner`, this.httpOptions);
  }

  uploadVideo(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<VideoBanner>(`${this.baseUrl}/admin/video-banner/video`, form, this.httpOptions);
  }

  uploadPoster(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<VideoBanner>(`${this.baseUrl}/admin/video-banner/poster`, form, this.httpOptions);
  }

  deleteVideo() {
    return this.http.delete<VideoBanner>(`${this.baseUrl}/admin/video-banner/video`, this.httpOptions);
  }

  deletePoster() {
    return this.http.delete<VideoBanner>(`${this.baseUrl}/admin/video-banner/poster`, this.httpOptions);
  }
}
