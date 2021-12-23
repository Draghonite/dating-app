export interface Photo {
    id: number;
    url: string;
    isMain: boolean;
    username?: string;
    isApproved: boolean;
}
