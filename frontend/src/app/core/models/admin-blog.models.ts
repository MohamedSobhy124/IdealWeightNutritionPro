export interface AdminBlogPostListItem {
  id: number;
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
  isDeleted: boolean;
}

export interface AdminBlogPostDetail extends AdminBlogPostListItem {
  content: string;
  contentAr: string;
  metaDescription?: string | null;
  metaDescriptionAr?: string | null;
  metaKeywords?: string | null;
  metaKeywordsAr?: string | null;
}

export interface UpsertAdminBlogPostRequest {
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
}
