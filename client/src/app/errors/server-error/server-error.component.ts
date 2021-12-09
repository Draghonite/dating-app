import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-server-error',
  templateUrl: './server-error.component.html',
  styleUrls: ['./server-error.component.css']
})
export class ServerErrorComponent implements OnInit {
  error: any;

  constructor(private router: Router) {
    const navigation = this.router.getCurrentNavigation();
    try {
      this.error = JSON.parse(navigation?.extras?.state?.error);
      console.log("[S-ERROR]", this.error);
    } catch(ex) {}
  }

  ngOnInit(): void {
  }

}
