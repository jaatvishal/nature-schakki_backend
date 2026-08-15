import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { AccountService } from '../../../core/services/account.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { Address } from '../../../shared/models/address';

@Component({
  selector: 'app-addresses',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatFormField, MatLabel, MatInput, MatButton, MatCheckbox, RouterLink],
  templateUrl: './addresses.component.html',
})
export class AddressesComponent implements OnInit {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private snackbar = inject(SnackbarService);

  addresses = signal<Address[]>([]);
  showForm = signal(false);
  editingId = signal<number | null>(null);

  addressForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    address1: ['', Validators.required],
    address2: [''],
    city: ['', Validators.required],
    state: ['', Validators.required],
    zipCode: ['', Validators.required],
    country: ['India', Validators.required],
    isDefault: [false],
  });

  ngOnInit(): void {
    this.loadAddresses();
  }

  loadAddresses() {
    this.accountService.getAddresses().subscribe({
      next: addresses => this.addresses.set(addresses),
      error: () => {},
    });
  }

  openAddForm() {
    this.editingId.set(null);
    this.addressForm.reset({ country: 'India', isDefault: false });
    this.showForm.set(true);
  }

  editAddress(address: Address) {
    this.editingId.set(address.id ?? null);
    this.addressForm.patchValue(address);
    this.showForm.set(true);
  }

  onSubmit() {
    if (this.addressForm.invalid) return;

    const data = this.addressForm.getRawValue() as Address;
    const id = this.editingId();

    const request = id
      ? this.accountService.updateAddress(id, data)
      : this.accountService.createAddress(data);

    request.subscribe({
      next: () => {
        this.snackbar.success(id ? 'Address updated' : 'Address added');
        this.showForm.set(false);
        this.loadAddresses();
      },
      error: () => this.snackbar.error('Failed to save address'),
    });
  }

  deleteAddress(id: number) {
    this.accountService.deleteAddress(id).subscribe({
      next: () => {
        this.snackbar.success('Address deleted');
        this.loadAddresses();
      },
      error: () => this.snackbar.error('Failed to delete address'),
    });
  }
}
