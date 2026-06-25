export interface PropertySummaryResponse {
  propertyId: string;
  title: string;
  monthlyRent: number;
  bedrooms: number;
  bathrooms: number;
  state: string;
  addressLine: string;
  primaryImage: string | null;
  propertyType: string;
  status: string;
}

export interface PropertyImageResponse {
  imageId: string;
  image: string;
  isPrimary: boolean;
  displayOrder: number;
}

export interface PropertyDetailResponse {
  propertyId: string;
  ownerId: string;
  ownerName: string;
  title: string;
  description: string;
  propertyType: string;
  locationId: string;
  state: string;
  district: string;
  pincode: string;
  country: string;
  addressLine: string;
  latitude: number | null;
  longitude: number | null;
  landArea: number | null;
  buildupArea: number | null;
  monthlyRent: number;
  securityDeposit: number;
  bedrooms: number;
  bathrooms: number;
  status: string;
  amenities: string[];
  images: PropertyImageResponse[];
}

export interface PropertyFilters {
  state?: string;
  propertyType?: string;
  bedrooms?: number;
  minRent?: number;
  maxRent?: number;
}
