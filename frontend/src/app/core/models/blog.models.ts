export interface BlogPostSummary {
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
}

export interface BlogPostDetail extends BlogPostSummary {
  content: string;
  contentAr: string;
  metaDescription?: string | null;
  metaDescriptionAr?: string | null;
  metaKeywords?: string | null;
  metaKeywordsAr?: string | null;
  relatedPosts: BlogPostSummary[];
}
