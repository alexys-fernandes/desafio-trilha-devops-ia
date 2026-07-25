import { Component, OnInit } from '@angular/core';
import { UserService } from '../../../services/user-service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-body',
  templateUrl: './body.html',
  styleUrl: './body.scss',
  standalone: false
})
export class Body implements OnInit {
  isLoggedIn$!: Observable<any>;

  constructor(private userService: UserService) { }

  ngOnInit(): void {
    this.isLoggedIn$ = this.userService.getUser();
  }
}
