import { Injectable, inject } from '@angular/core';

import { HttpClient } from '@angular/common/http';

import { environment } from '../../../environments/environment';

import {

  NewsletterSubscribeResponse,

  NewsletterStatusResponse,

  NewsletterSubscription,

  NewsletterUnsubscribeRequest,

  NewsletterUnsubscribeResponse,

} from '../models/newsletter.models';



@Injectable({ providedIn: 'root' })

export class NewsletterService {

  private readonly http = inject(HttpClient);

  private readonly baseUrl = environment.apiBaseUrl;



  subscribe(email?: string, source = 'Storefront') {

    return this.http.post<NewsletterSubscribeResponse>(

      `${this.baseUrl}/newsletter/subscribe`,

      { email, source },

      { withCredentials: true }

    );

  }



  getStatus(email?: string) {

    return this.http.get<NewsletterStatusResponse>(`${this.baseUrl}/newsletter/status`, {

      params: email ? { email } : {},

      withCredentials: true,

    });

  }



  unsubscribe(email?: string) {

    const body: NewsletterUnsubscribeRequest = email ? { email } : {};

    return this.http.post<NewsletterUnsubscribeResponse>(

      `${this.baseUrl}/newsletter/unsubscribe`,

      body,

      { withCredentials: true }

    );

  }



  listAdmin(status = 'all') {

    return this.http.get<NewsletterSubscription[]>(`${this.baseUrl}/admin/newsletter`, {

      params: { status },

      withCredentials: true,

    });

  }



  toggleActive(id: number) {

    return this.http.post(`${this.baseUrl}/admin/newsletter/${id}/toggle`, {}, {

      withCredentials: true,

    });

  }



  delete(id: number) {

    return this.http.delete(`${this.baseUrl}/admin/newsletter/${id}`, { withCredentials: true });

  }



  exportActive() {

    return this.http.get(`${this.baseUrl}/admin/newsletter/export`, {

      responseType: 'blob',

      withCredentials: true,

    });

  }

}

