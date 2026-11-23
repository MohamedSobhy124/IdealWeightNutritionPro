import { DatePipe, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { BlogService } from '../../core/services/blog.service';
import { CatalogueService } from '../../core/services/catalogue.service';
import { LocaleService } from '../../core/services/locale.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { BlogPostSummary } from '../../core/models/blog.models';
import {
  UiArticleCardComponent,
  UiEmptyStateComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
} from '../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    DatePipe,
    UiPageHeaderComponent,
    UiArticleCardComponent,
    UiSkeletonComponent,
    UiEmptyStateComponent,
  ],
  templateUrl: './blog-list.component.html',
  styleUrl: './blog-list.component.css',
})
export class BlogListComponent implements OnInit {
  private readonly blog = inject(BlogService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly posts = signal<BlogPostSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  displayTitle(post: BlogPostSummary): string {
    return this.locale.pick(post.title, post.titleAr);
  }

  displayExcerpt(post: BlogPostSummary): string {
    return this.locale.pick(post.excerpt, post.excerptAr);
  }

  displayCategory(post: BlogPostSummary): string {
    return this.locale.pick(post.category, post.categoryAr);
  }

  displayAuthor(post: BlogPostSummary): string {
    return this.locale.pick(post.author, post.authorAr);
  }

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.blog.listPosts().subscribe({
      next: (posts) => {
        this.posts.set(posts);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('blogLoadError'));
        this.loading.set(false);
      },
    });
  }
}
