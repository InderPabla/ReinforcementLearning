#include "stdafx.h"
#include <iostream>

#include "floatfann.h"
#include "fann_cpp.h"

using namespace std;

class NetLoader {

	private:
		fann *ann;
		const char *netname;
		int epoch_save = 100;
		int epoch_counter = 0;

	public:
		// --- TEST CONSTRUCTOR ---
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

		NetLoader(const char *netname, int epoch_save, float learning_rate) {
			this->netname = netname;
			this->epoch_save = epoch_save;
			ann = fann_create_from_file(this->netname);
			fann_set_learning_rate(ann, learning_rate);
		}

		void fit(fann_train_data *data, int epochs, int reports) {
			//fann_train_on_data(ann, data, max_epochs, epochs_between_reports, desired_error);
			fann_train_on_data(ann, data, epochs, reports, 0.0f);
			fann_save(ann, netname);
		}

		void fit(fann_type *input, fann_type *output) {
			fann_train(ann, input, output);
			epoch_counter++;
			if (epoch_counter == epoch_save) {
				epoch_counter = 0;
				printf("Saving Net");
				fann_save(ann, netname);
			}
		}

		fann_type* predict(fann_type input[]) {
			fann_type *calc_out = fann_run(ann, input);
			return calc_out;
		}

		
};