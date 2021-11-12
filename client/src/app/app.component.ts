import { HttpClient } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'The Dating App';
  users: any;
  subs: Array<any> = [];

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.getUsers()
  }

  getUsers() {
    this.subs.push(this.http.get("https://localhost:5001/api/users").subscribe(response => {
      this.users = response;
    }, error => {
      console.log(error);
    }));
  }

  ngOnDestroy() {
    console.log("Checking subscriptions");
    if (this.subs && this.subs.length > 0) {
      console.log(`Unsubscribing ${this.subs.length} subscriptions`);
      this.subs.map(sub => {
        try {
          sub.unsubscribe();
        } catch(ex) {}
      })
    }
  }
}
