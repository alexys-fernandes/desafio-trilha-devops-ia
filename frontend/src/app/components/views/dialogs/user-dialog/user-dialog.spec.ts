import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import { UserResponse } from '../../../../models/user-responde.model';
import { UserService } from '../../../../services/user-service';
import { UserDialog } from './user-dialog';

describe('UserDialog', () => {
  let fixture: ComponentFixture<UserDialog>;

  const user: UserResponse = {
    id: 7,
    name: 'Victor Mendes',
    email: 'victor@example.com',
  };

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [UserDialog],
      imports: [CommonModule, FormsModule, MatDialogModule],
      providers: [
        {
          provide: MatDialogRef,
          useValue: {
            close: vi.fn(),
          },
        },
        {
          provide: MAT_DIALOG_DATA,
          useValue: user,
        },
        {
          provide: UserService,
          useValue: {
            update: vi.fn(() => of({ ...user })),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserDialog);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('renders injected user name and email in the form fields', () => {
    const nameInput = view().querySelector('[data-testid="user-name-input"]') as HTMLInputElement;
    const emailInput = view().querySelector('[data-testid="user-email-input"]') as HTMLInputElement;

    expect(nameInput.value).toBe(user.name);
    expect(emailInput.value).toBe(user.email);
  });
});
