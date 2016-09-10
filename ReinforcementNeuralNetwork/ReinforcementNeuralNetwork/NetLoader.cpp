#include "stdafx.h"
#include <iostream>

#include "floatfann.h"
#include "fann_cpp.h"

using namespace std;

class NetLoader {

	private:
		fann *ann;

	public:
		NetLoader(const char *netname, const char *dataname, bool trainMode) {
			ann = fann_create_from_file(netname);
			
			const float desired_error = (const float) 0.000001;
			const unsigned int max_epochs = 500000;
			const unsigned int epochs_between_reports = 1000;

			if (trainMode == true) {
				fann_train_on_file(ann, dataname, max_epochs, epochs_between_reports, desired_error);
				fann_save(ann, netname);
				fann_destroy(ann);
			}
			else {

				fann_type *calc_out;
				struct fann_train_data *data;
				data = fann_read_train_from_file(dataname);
				printf("Data Size: %i \n",sizeof(data));

				for (int i = 0; i < fann_length_train_data(data); i++)
				{
					fann_reset_MSE(ann);
					calc_out = fann_test(ann, data->input[i], data->output[i]);

					printf("XOR test (%f, %f) -> %f, should be %f, difference=%f\n",
						data->input[i][0], data->input[i][1], calc_out[0], data->output[i][0],
						(float)fann_abs(calc_out[0] - data->output[i][0]));
				}

				printf("Cleaning up.\n");
				fann_destroy_train(data);
				fann_destroy(ann);
			}


		}

		
};