import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { UpsertAdminBlogPostRequest } from '../../../core/models/admin-blog.models';
import { AdminBlogService } from '../../../core/services/admin-blog.service';
import { AdminMediaService } from '../../../core/services/admin-service.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import { RichTextEditorComponent } from '../../../shared/components/rich-text-editor/rich-text-editor.component';
import {
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    RichTextEditorComponent,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiSkeletonComponent,
    UiEmptyStateComponent,
  ],

  templateUrl: './admin-blog-form.component.html',

  styleUrl: './admin-blog-form.component.css',

})

export class AdminBlogFormComponent implements OnInit {
  private readonly blogApi = inject(AdminBlogService);
  private readonly mediaApi = inject(AdminMediaService);
  readonly catalogue = inject(CatalogueService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly uploadingImage = signal(false);

  readonly error = signal<string | null>(null);

  readonly saveError = signal<string | null>(null);

  readonly isEdit = signal(false);



  blogId: number | null = null;



  slug = '';

  title = '';

  titleAr = '';

  category = '';

  categoryAr = '';

  author = '';

  authorAr = '';

  publishedDate = '';

  readTime = 5;

  imageUrl = '';

  excerpt = '';

  excerptAr = '';

  content = '';

  contentAr = '';

  metaDescription = '';

  metaDescriptionAr = '';

  metaKeywords = '';

  metaKeywordsAr = '';



  t(key: AdminUiKey): string {

    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);

  }



  ngOnInit(): void {

    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam && idParam !== 'new') {

      const id = Number(idParam);

      if (!id) {

        this.error.set(this.t('invalidBlogPost'));

        this.loading.set(false);

        return;

      }

      this.isEdit.set(true);

      this.blogId = id;

      this.blogApi.get(id).subscribe({

        next: (post) => {

          this.applyPost(post);

          this.loading.set(false);

        },

        error: () => {

          this.error.set(this.t('blogPostNotFound'));

          this.loading.set(false);

        },

      });

    } else {

      this.publishedDate = this.toDateInput(new Date().toISOString());

      this.loading.set(false);

    }

  }



  private applyPost(post: {

    slug: string;

    title: string;

    titleAr: string;

    category: string;

    categoryAr: string;

    author: string;

    authorAr: string;

    publishedDate: string;

    readTime: number;

    imageUrl?: string | null;

    excerpt: string;

    excerptAr: string;

    content: string;

    contentAr: string;

    metaDescription?: string | null;

    metaDescriptionAr?: string | null;

    metaKeywords?: string | null;

    metaKeywordsAr?: string | null;

  }): void {

    this.slug = post.slug;

    this.title = post.title;

    this.titleAr = post.titleAr;

    this.category = post.category;

    this.categoryAr = post.categoryAr;

    this.author = post.author;

    this.authorAr = post.authorAr;

    this.publishedDate = this.toDateInput(post.publishedDate);

    this.readTime = post.readTime;

    this.imageUrl = post.imageUrl ?? '';

    this.excerpt = post.excerpt;

    this.excerptAr = post.excerptAr;

    this.content = post.content;

    this.contentAr = post.contentAr;

    this.metaDescription = post.metaDescription ?? '';

    this.metaDescriptionAr = post.metaDescriptionAr ?? '';

    this.metaKeywords = post.metaKeywords ?? '';

    this.metaKeywordsAr = post.metaKeywordsAr ?? '';

  }



  submit(): void {

    if (

      !this.title.trim() ||

      !this.titleAr.trim() ||

      !this.category.trim() ||

      !this.categoryAr.trim() ||

      !this.author.trim() ||

      !this.authorAr.trim() ||

      !this.excerpt.trim() ||

      !this.excerptAr.trim() ||

      !this.hasContent(this.content) ||

      !this.hasContent(this.contentAr)

    ) {

      this.saveError.set(this.t('blogPostRequiredFields'));

      return;

    }



    const slug = this.slug.trim() || this.slugify(this.title);

    const request: UpsertAdminBlogPostRequest = {

      slug,

      title: this.title.trim(),

      titleAr: this.titleAr.trim(),

      category: this.category.trim(),

      categoryAr: this.categoryAr.trim(),

      author: this.author.trim(),

      authorAr: this.authorAr.trim(),

      publishedDate: new Date(this.publishedDate).toISOString(),

      readTime: this.readTime,

      imageUrl: this.imageUrl.trim() || null,

      excerpt: this.excerpt.trim(),

      excerptAr: this.excerptAr.trim(),

      content: this.content.trim(),

      contentAr: this.contentAr.trim(),

      metaDescription: this.metaDescription.trim() || null,

      metaDescriptionAr: this.metaDescriptionAr.trim() || null,

      metaKeywords: this.metaKeywords.trim() || null,

      metaKeywordsAr: this.metaKeywordsAr.trim() || null,

    };



    this.submitting.set(true);

    this.saveError.set(null);



    const op =

      this.isEdit() && this.blogId != null

        ? this.blogApi.update(this.blogId, request)

        : this.blogApi.create(request);



    op.subscribe({

      next: (post) => {

        this.submitting.set(false);

        if (!this.isEdit()) {

          void this.router.navigate(['/admin/blog', post.id]);

        }

      },

      error: (err) => {

        this.saveError.set(err?.error?.errors?.[0] ?? this.t('saveFailed'));

        this.submitting.set(false);

      },

    });

  }



  private slugify(text: string): string {

    return text

      .toLowerCase()

      .trim()

      .replace(/[^\w\s-]/g, '')

      .replace(/\s+/g, '-');

  }



  private toDateInput(iso: string): string {
    return iso.slice(0, 10);
  }

  onImageSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || !this.blogId) return;

    this.uploadingImage.set(true);
    this.mediaApi.uploadBlogPostImage(this.blogId, file).subscribe({
      next: (res) => {
        this.imageUrl = res.imageUrl;
        this.uploadingImage.set(false);
      },
      error: (err) => {
        this.saveError.set(err?.error?.errors?.[0] ?? this.t('uploadFailed'));
        this.uploadingImage.set(false);
      },
    });
    (event.target as HTMLInputElement).value = '';
  }

  private hasContent(html: string): boolean {
    return html.replace(/<[^>]*>/g, '').replace(/&nbsp;/g, ' ').trim().length > 0;
  }
}


