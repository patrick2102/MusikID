import React, { Component } from 'react';

export class CheckFiles extends Component {
    static displayName = CheckFiles.name;

  constructor(props) {
    super(props);
      this.state = {
          results: [],
          count: 0,
          completed_count: 0,
          loading: false,
          folder_name: "",
          folder_name_post: ""
      };

      this.postCheckFiles = this.postCheckFiles.bind(this);
      this.getCheckFilesResult = this.getCheckFilesResult.bind(this);
      this.changeFolderName = this.changeFolderName.bind(this);
      this.changeFolderNamePost = this.changeFolderNamePost.bind(this);
  }




  static renderFilesTable(results) {
      return (
          <div>
              
      <table className='table table-striped' aria-labelledby="tabelLabel">
        <thead>
          <tr>
            <th>File Path</th>
            <th>Found in fingerprint database</th>
            <th>Reference</th>
          </tr>
        </thead>
        <tbody>
          {results.map(f =>
            <tr key={f.file_path}>
                  <td>{f.file_path}</td>
                  <td> {f.found ? "true" : "false"} </td>
              <td>{f.reference}</td>
        
            </tr>
          )}
        </tbody>
              </table>
              </div>
    );
    }

    
    changeFolderName(event) {
        this.setState({ folder_name: event.target.value });
    }

    changeFolderNamePost(event) {
        this.setState({ folder_name_post: event.target.value });
    }
    changeUser(event) {
        this.setState({ user: event.target.value });
    }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : CheckFiles.renderFilesTable(this.state.results);

    return (
      <div>
            <h5 id="tabelLabel" >Track Folder Check</h5>
            
            <form onSubmit={this.postCheckFiles}>
                <p>UPLOAD</p>
                <label>
                    Folder name:
                <input type="text" size="100" name="Folder Name" onChange={this.changeFolderNamePost} />
                </label>
                <br />

                <input type="submit" text="Post folder names for check files analysis" title = "Post folder" />
            </form> 

            <form onSubmit={this.getCheckFilesResult}>
                <p>GET</p>
                <label>
                    Folder name:
                <input type="text" size="100" name="File Path" onChange={this.changeFolderName} title="Get folder"/>
                </label>
                <br/>

                <input type="submit" text="Post file for analysis" />
            </form> 



            <br />
            <div>
                 {this.state.completed_count}/{this.state.count} of files analyzed.
            </div>
            <br />
            {contents}
      </div>
    );
  }

    

    async postCheckFiles(e) {
        e.preventDefault();
        const input = { "folder_name": this.state.folder_name_post };
        const settings = {
            method: "post",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(input)
        };
        const url = "api/TrackFolderCheck/";

        const response = await fetch(url, settings);
        const data = await response.text();

        this.setState({ loading: false });
        alert("A folder was submitted for check files analysis: " + data);
    }

    async getCheckFilesResult(event) {
        event.preventDefault();
        this.setState({ loading: true });
        const settings = {
            method: "get",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        };
        const url = "api/TrackFolderCheck/" + this.state.folder_name;

        const response = await fetch(url, settings);
        const json = await response.json();

        this.setState({
            results: json.file_results,
            count: json.file_count,
            completed_count: json.file_completed_count,
            loading: false
        });
        //  alert("A file was submitted for analysis: " + data);
    }

    async FetchFiles() {

        const response = await fetch('api/files');
        const data = await response.json();
        this.setState({ files: data, loading: false });
    }
}
