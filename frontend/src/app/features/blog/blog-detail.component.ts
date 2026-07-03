import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BlogService } from '../../core/services/blog.service';
import { CatalogueService } from '../../core/services/catalogue.service';
import { LocaleService } from '../../core/services/locale.service';
import { SeoService } from '../../core/services/seo.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { BlogPostDetail, BlogPostSummary } from '../../core/models/blog.models';
import { UiArticleCardComponent, UiBadgeComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [RouterLink, DatePipe, UiBadgeComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent, UiArticleCardComponent],
  templateUrl: './blog-detail.component.html',
  styleUrl: './blog-detail.component.css',
})
export class BlogDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly blog = inject(BlogService);
  private readonly seo = inject(SeoService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly post = signal<BlogPostDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  displayTitle(post: BlogPostDetail | BlogPostSummary): string {
    return this.locale.pick(post.title, post.titleAr);
  }

  displayContent(post: BlogPostDetail): string {
    return this.locale.pick(post.content, post.contentAr);
  }

  displayCategory(post: BlogPostDetail): string {
    return this.locale.pick(post.category, post.categoryAr);
  }

  displayAuthor(post: BlogPostDetail): string {
    return this.locale.pick(post.author, post.authorAr);
  }

  displayExcerpt(post: BlogPostSummary): string {
    return this.locale.pick(post.excerpt, post.excerptAr);
  }

  displayCategorySummary(post: BlogPostSummary): string {
    return this.locale.pick(post.category, post.categoryAr);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const slug = params.get('slug');
      if (!slug) {
        this.error.set(this.t('articleNotFound'));
        this.loading.set(false);
        return;
      }

      this.loading.set(true);
      this.error.set(null);
      this.blog.getPost(slug).subscribe({
        next: (post) => {
          this.post.set(post);
          this.seo.applyPage({
            title: post.title,
            titleAr: post.titleAr,
            description: post.metaDescription ?? post.excerpt,
            descriptionAr: post.metaDescriptionAr ?? post.excerptAr,
            imageUrl: post.imageUrl,
            path: `/blog/${post.slug}`,
            type: 'article',
          });
          this.loading.set(false);
        },
        error: () => {
          this.error.set(this.t('articleNotFound'));
          this.loading.set(false);
        },
      });
    });
  }
}
