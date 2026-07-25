import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { ConfirmDialog, ConfirmDialogData } from './confirm-dialog';

describe('ConfirmDialog', () => {
  let fixture: ComponentFixture<ConfirmDialog>;
  let close: ReturnType<typeof vi.fn>;

  const dialogData: ConfirmDialogData = {
    title: 'Excluir hábito',
    message: 'Tem certeza que deseja excluir este hábito?',
    confirmBtnText: 'Excluir',
  };

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    close = vi.fn();

    await TestBed.configureTestingModule({
      declarations: [ConfirmDialog],
      imports: [CommonModule, MatDialogModule],
      providers: [
        {
          provide: MatDialogRef,
          useValue: {
            close,
          },
        },
        {
          provide: MAT_DIALOG_DATA,
          useValue: dialogData,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmDialog);
    fixture.detectChanges();
  });

  it('closes with false when cancel is clicked', () => {
    const cancelButton = view().querySelector(
      '[data-testid="confirm-cancel-button"]',
    ) as HTMLButtonElement;

    cancelButton.click();

    expect(close).toHaveBeenCalledWith(false);
  });

  it('closes with true when confirm is clicked', () => {
    const confirmButton = view().querySelector(
      '[data-testid="confirm-action-button"]',
    ) as HTMLButtonElement;

    confirmButton.click();

    expect(close).toHaveBeenCalledWith(true);
  });
});
