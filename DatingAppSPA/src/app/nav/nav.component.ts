import { Component, OnInit } from '@angular/core';
import { AuthService } from '../_services/auth.service';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {
  userLoginModel: any = {};
  username: string;

  constructor(private authService: AuthService) { }

  ngOnInit() {
  }

  userLogin() {
    this.authService.userLogin(this.userLoginModel).subscribe( next => {
      this.username = this.userLoginModel.username;
      console.log('Success');
    }, err => { console.log(err);
    });
  }

  loggedIn() {
    const token = localStorage.getItem('token');
    return !!token;
  }

  logout() {
    localStorage.removeItem('token');
    console.log('Logged out!');
  }

}
