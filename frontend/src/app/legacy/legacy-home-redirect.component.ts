import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

/** Handles /Customer/Home with optional categoryId query (legacy shop filter). */
@Component({
  standalone: true,
  template: '',
})
export class LegacyHomeRedirectComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const categoryId = this.route.snapshot.queryParamMap.get('categoryId');
    const queryParams: Record<string, string> = {};
    for (const key of ['brandId', 'search', 'sortBy', 'page', 'pageSize'] as const) {
      const value = this.route.snapshot.queryParamMap.get(key);
      if (value) {
        queryParams[key] = value;
      }
    }

    if (categoryId) {
      queryParams['categoryId'] = categoryId;
      void this.router.navigate(['/shop'], { queryParams, replaceUrl: true });
      return;
    }

    void this.router.navigate(['/'], { queryParams, replaceUrl: true });
  }
}
