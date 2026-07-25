import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { UserResponse } from '../../../../models/user-responde.model';
import { User } from '../../../../models/user.model';
import { UserService } from '../../../../services/user-service';

@Component({
  selector: 'app-user-dialog',
  templateUrl: './user-dialog.html',
  styleUrl: './user-dialog.scss',
  standalone: false
})
export class UserDialog implements OnInit {
  formData: User = {
    id: 0,
    name: '',
    email: '',
    password: '',
    createdAt: new Date(),
    isDeleted: false
  };

  constructor(
    public dialogRef: MatDialogRef<UserDialog>,
    @Inject(MAT_DIALOG_DATA) public data: UserResponse,
    private userService: UserService
  ) { }

  ngOnInit(): void {
    if (this.data) {
      this.formData = {
        ...this.formData,
        id: this.data.id,
        name: this.data.name,
        email: this.data.email
      };
    }
  }

  onSave(): void {
    if (this.formData.name && this.formData.email) {
      const payload = { ...this.formData };

      if (!payload.password || payload.password.trim() === '') {
        payload.password = '';
      }

      this.userService.update(payload).subscribe({
        next: () => {
          this.dialogRef.close();
        },
        error: (err) => {
          console.error('Erro ao atualizar dados: ', err);
        }
      });
    }
  }

  onClose(): void {
    this.dialogRef.close();
  }
}
