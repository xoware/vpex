package com.xoware.vpexconnectionmanager;

import android.app.Activity;
import android.app.DialogFragment;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.TextView;

public class DeleteConfirmationDialogFragment extends DialogFragment {

    public static final String ARG_ITEM_ID = "item_id";

    String connectionName;
    
	public interface DeleteConfirmationCallback {
		public void confirmDeletionOfConnection(String connection, boolean confirmed);
	}

	DeleteConfirmationCallback mCallback;

	public static final String TAG = "VPExConnectionManager";

	TextView message;
	Button yesButton, noButton;

	static DeleteConfirmationDialogFragment newInstance() {
		return new DeleteConfirmationDialogFragment();
	}
	
	@Override
    public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
        if (getArguments().containsKey(ARG_ITEM_ID)) {
        	connectionName = getArguments().getString(ARG_ITEM_ID);
        }
	}

	@Override
	public View onCreateView(LayoutInflater inflater, ViewGroup container,
			Bundle savedInstanceState) {
		getDialog().setTitle("Confirm Deletion");

		View dialogView = inflater.inflate(R.layout.delete_confirmation_dialog,
				container, false);
		getDialog().setCanceledOnTouchOutside(false);
		message = (TextView) dialogView.findViewById(R.id.message);
		message.setText("Are you sure you wish to delete the connection '" + connectionName + "'?");
		yesButton = (Button) dialogView.findViewById(R.id.yes_button);
		noButton = (Button) dialogView.findViewById(R.id.no_button);
		yesButton.setOnClickListener(yesButtonOnClickListener);
		noButton.setOnClickListener(noButtonOnClickListener);

		return dialogView;
	}

	@Override
	public void onAttach(Activity activity) {

		super.onAttach(activity);
		try {
			mCallback = (DeleteConfirmationCallback) activity;
		} catch (ClassCastException e) {
			throw new ClassCastException(activity.toString()
					+ " must implement " + DeleteConfirmationCallback.class.getName());
		}
	}

	private Button.OnClickListener yesButtonOnClickListener = new Button.OnClickListener() {

		@Override
		public void onClick(View arg0) {
			mCallback.confirmDeletionOfConnection(connectionName, true);
			getDialog().dismiss();
		}
	};

	private Button.OnClickListener noButtonOnClickListener = new Button.OnClickListener() {

		@Override
		public void onClick(View arg0) {
			mCallback.confirmDeletionOfConnection(connectionName, false);
			getDialog().dismiss();
		}
	};
}
