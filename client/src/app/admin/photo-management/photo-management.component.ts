import { Component, OnInit } from '@angular/core';
import { AdminService } from 'src/app/services/admin.service';
import { Photo } from 'src/app/_models/photo';

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.css']
})
export class PhotoManagementComponent implements OnInit {
  photos: Photo[];

  constructor(private adminService: AdminService) { }

  ngOnInit(): void {
    this.getPhotosForApproval();
  }

  getPhotosForApproval() {
    this.adminService.getPhotosForApproval().subscribe((photos: Photo[]) => {
      this.photos = photos;
    });
  }

  approvePhoto(id: number) {
    this.adminService.approvePhoto(id).subscribe(() => {
      this.getPhotosForApproval();
    });
  }

  rejectPhoto(id: number) {
    this.adminService.rejectPhoto(id).subscribe(() => {
      this.getPhotosForApproval();
    });
  }

}
